#region imports
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Indicators;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Securities;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    // ==================================================================================
    // 策略版本：V24.2 五标的全天候蓝筹组合
    // 核心组合：JNJ (医疗), PG (必需消费), CVX (能源), DUK (公用事业), MSFT (科技)
    // 策略思维：通过行业互补降低共振回撤，利用个性化均线和分批入场获取中长期波段收益。
    // ==================================================================================
    public class MultiSymbolSwingV24_2 : QCAlgorithm
    {
        // --- 数据容器 ---
        public class SymbolData
        {
            public Symbol Symbol;
            public SimpleMovingAverage SMA;
            public List<Lot> Lots = new List<Lot>();
            public bool BoughtToday;
            public bool SoldToday;

            // 个性化配置参数
            public int SMALength;
            public decimal BuyThreshold;
            public decimal BuyFraction;
            public decimal TrailingDrop;
            public decimal TakeProfitUp;

            public class Lot
            {
                public int Quantity; public decimal EntryPrice; public decimal HighestPrice;
                public bool TrailingActive; public bool PendingSell;
            }
        }

        // --- 全局参数 ---
        private Dictionary<Symbol, SymbolData> _data = new Dictionary<Symbol, SymbolData>();

        // 【核心风控】
        // 5只股票平均每只18%，总持仓上限90%，永远预留10%现金应对极端行情或定投
        private decimal _maxPerStockPct = 0.18m;
        private decimal _minBuyAmount = 2000m;   // 调低最小金额以适应 0.08 的分段入场
        private DateTime _lastActionDate = DateTime.MinValue;
        private DateTime _lastContributionMonth = DateTime.MinValue;

        public override void Initialize()
        {
            // 1. 回测基础配置
            SetStartDate(2015, 1, 1);
            SetEndDate(2025, 1, 1);
            SetCash(50000);

            // 2. 针对 5 只股票的个性化配置字典
            var configs = new Dictionary<string, Action<SymbolData>> {
                { "JNJ",  s => { s.SMALength = 240; s.BuyThreshold = 0.05m; s.BuyFraction = 0.08m; s.TrailingDrop = 0.03m; s.TakeProfitUp = 0.10m; }},
                { "PG",   s => { s.SMALength = 200; s.BuyThreshold = 0.04m; s.BuyFraction = 0.08m; s.TrailingDrop = 0.02m; s.TakeProfitUp = 0.08m; }},
                { "CVX",  s => { s.SMALength = 150; s.BuyThreshold = 0.08m; s.BuyFraction = 0.08m; s.TrailingDrop = 0.05m; s.TakeProfitUp = 0.12m; }},
                { "DUK",  s => { s.SMALength = 200; s.BuyThreshold = 0.05m; s.BuyFraction = 0.08m; s.TrailingDrop = 0.02m; s.TakeProfitUp = 0.08m; }},
                { "MSFT", s => { s.SMALength = 150; s.BuyThreshold = 0.08m; s.BuyFraction = 0.08m; s.TrailingDrop = 0.04m; s.TakeProfitUp = 0.15m; }}
            };

            // 3. 循环初始化标的
            foreach (var ticker in configs.Keys)
            {
                var equity = AddEquity(ticker, Resolution.Daily);
                equity.SetDataNormalizationMode(DataNormalizationMode.Raw); // 使用原始价格计算，更符合分红定投逻辑
                var symbol = equity.Symbol;

                var sd = new SymbolData { Symbol = symbol };
                configs[ticker](sd);

                // 动态绑定指标
                sd.SMA = SMA(symbol, sd.SMALength, Resolution.Daily);
                _data[symbol] = sd;
            }

            // 4. 指标预热
            SetWarmUp(240, Resolution.Daily);
        }

        public override void OnData(Slice data)
        {
            if (IsWarmingUp) return;

            // 每日清理状态
            if (Time.Date != _lastActionDate.Date)
            {
                _lastActionDate = Time.Date;
                foreach (var sd in _data.Values) { sd.BoughtToday = false; sd.SoldToday = false; }
            }

            // 执行每月定投 $500，增加现金流
            HandleMonthlyContribution(500);

            // 轮询处理组合中的每只股票
            foreach (var sd in _data.Values)
            {
                if (!data.Bars.ContainsKey(sd.Symbol)) continue;
                var price = data.Bars[sd.Symbol].Close;
                if (!sd.SMA.IsReady) continue;

                // A. 卖出逻辑 (包含硬止损与移动止盈)
                HandleSellLogic(sd, price);

                // B. 买入逻辑 (只有当天没卖出且未超重时才买)
                if (!sd.SoldToday) HandleBuyLogic(sd, price);
            }
        }

        private void HandleBuyLogic(SymbolData sd, decimal price)
        {
            if (sd.BoughtToday) return;

            // 单股持仓权重检查：防止某只股票在下跌过程中过度消耗资金
            var currentHoldings = Portfolio[sd.Symbol].HoldingsValue;
            var totalValue = Portfolio.TotalPortfolioValue;
            if (currentHoldings >= totalValue * _maxPerStockPct) return;

            // 动态资金分配：每次动用当前现金的 8%
            var amountToInvest = Portfolio.Cash * sd.BuyFraction;
            if (amountToInvest < _minBuyAmount) return;

            // 基于个性化均线的抄底判定
            var triggerPrice = sd.SMA.Current.Value * (1 - sd.BuyThreshold);

            if (price < triggerPrice)
            {
                var qty = (int)(amountToInvest / price);
                if (qty > 0)
                {
                    MarketOrder(sd.Symbol, qty);
                    sd.BoughtToday = true;
                    //Debug($"[{Time:yyyy-MM-dd}] BUY {sd.Symbol}: Price({price:F2}) < Trigger({triggerPrice:F2})");
                }
            }
        }

        private void HandleSellLogic(SymbolData sd, decimal price)
        {
            foreach (var lot in sd.Lots.Where(l => !l.PendingSell).ToList())
            {
                // 更新每笔订单持仓期间的最高价
                if (price > lot.HighestPrice) lot.HighestPrice = price;

                // 1. 硬性止损保护：4%
                bool shouldSell = (price <= lot.EntryPrice * 0.96m);

                // 2. 移动止盈逻辑
                // 先判断是否达到开启移动止盈的起涨点
                if (!lot.TrailingActive && price >= lot.EntryPrice * (1 + sd.TakeProfitUp))
                {
                    lot.TrailingActive = true;
                }

                // 如果已开启移动止盈，且价格回落超过 TrailingDrop，则卖出
                if (lot.TrailingActive && price <= lot.HighestPrice * (1 - sd.TrailingDrop))
                {
                    shouldSell = true;
                }

                if (shouldSell && lot.Quantity > 0)
                {
                    MarketOrder(sd.Symbol, -lot.Quantity);
                    lot.PendingSell = true;
                    sd.SoldToday = true;
                    //Debug($"[{Time:yyyy-MM-dd}] SELL {sd.Symbol}: Exit at {price:F2}. Status: {(price < lot.EntryPrice ? "Loss" : "Profit")}");
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled) return;
            if (!_data.TryGetValue(orderEvent.Symbol, out var sd)) return;

            if (orderEvent.Direction == OrderDirection.Buy)
            {
                // 买入成功：在独立账本中记录这一“批次”
                sd.Lots.Add(new SymbolData.Lot
                {
                    Quantity = (int)orderEvent.FillQuantity,
                    EntryPrice = orderEvent.FillPrice,
                    HighestPrice = orderEvent.FillPrice
                });
            }
            else
            {
                // 卖出成功：按买入顺序（FIFO）移除对应的账本记录
                var qtyToRem = (int)Math.Abs(orderEvent.FillQuantity);
                foreach (var lot in sd.Lots.OrderBy(l => l.EntryPrice).ToList())
                {
                    if (qtyToRem <= 0) break;
                    var take = Math.Min(lot.Quantity, qtyToRem);
                    lot.Quantity -= take; qtyToRem -= take;
                }
                sd.Lots.RemoveAll(l => l.Quantity <= 0);
            }
        }

        private void HandleMonthlyContribution(decimal amount)
        {
            // 每月注资逻辑
            if (Time.Month != _lastContributionMonth.Month)
            {
                _lastContributionMonth = Time;
                Portfolio.CashBook["USD"].AddAmount(amount);
            }
        }
    }
}
