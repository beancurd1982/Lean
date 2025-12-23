#region imports
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Indicators;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Parameters;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    // 版本 V30: 八星组合定稿版 (Eight-Star Portfolio Final)
    // 集成标的：MSFT, CVX, DUK, JNJ, PG, JPM, NVDA, AVGO
    public class MultiStockV30_Final : QCAlgorithm
    {
        public class SymbolSettings
        {
            public int SmaLength;
            public decimal BuyThreshold;
            public decimal TakeProfitUp;
            public decimal StopLoss;
            public decimal TrailingDrop;
            public decimal MaxWeight; // 新增：独立权重控制
        }

        public class SymbolData
        {
            public Symbol Symbol;
            public SimpleMovingAverage Sma;
            public SymbolSettings Settings;
            public List<Lot> Lots = new List<Lot>();
            public bool ActionToday = false;
        }

        public class Lot
        {
            public int Quantity; public decimal EntryPrice; public decimal HighestPrice;
            public bool TrailingActive; public bool PendingSell;
        }

        // --- 全局控制参数 ---
        [Parameter("rebalance-threshold")]
        private decimal _rebalanceThreshold = 0.02m; // 调仓死区

        [Parameter("buy-step")]
        private decimal _buyStep = 0.05m; // 每次加仓占总资产比例

        [Parameter("monthly-contribution")]
        private decimal _monthlyContribution = 1000m; // 每月定投金额

        // --- 八星参数配置表 ---
        private Dictionary<string, SymbolSettings> _config = new Dictionary<string, SymbolSettings> {
            { "MSFT", new SymbolSettings { SmaLength = 200, StopLoss = 0.04m, BuyThreshold = 0.04m, TakeProfitUp = 0.10m, TrailingDrop = 0.05m, MaxWeight = 0.14m } },
            { "CVX",  new SymbolSettings { SmaLength = 150, StopLoss = 0.08m, BuyThreshold = 0.10m, TakeProfitUp = 0.05m, TrailingDrop = 0.02m, MaxWeight = 0.12m } },
            { "DUK",  new SymbolSettings { SmaLength = 175, StopLoss = 0.05m, BuyThreshold = 0.05m, TakeProfitUp = 0.03m, TrailingDrop = 0.01m, MaxWeight = 0.12m } },
            { "JNJ",  new SymbolSettings { SmaLength = 240, StopLoss = 0.04m, BuyThreshold = 0.04m, TakeProfitUp = 0.10m, TrailingDrop = 0.01m, MaxWeight = 0.13m } },
            { "PG",   new SymbolSettings { SmaLength = 225, StopLoss = 0.05m, BuyThreshold = 0.02m, TakeProfitUp = 0.10m, TrailingDrop = 0.03m, MaxWeight = 0.13m } },
            { "JPM",  new SymbolSettings { SmaLength = 240, StopLoss = 0.05m, BuyThreshold = 0.02m, TakeProfitUp = 0.06m, TrailingDrop = 0.025m, MaxWeight = 0.13m } },
            { "NVDA", new SymbolSettings { SmaLength = 120, StopLoss = 0.10m, BuyThreshold = 0.08m, TakeProfitUp = 0.25m, TrailingDrop = 0.09m, MaxWeight = 0.11m } },
            { "AVGO", new SymbolSettings { SmaLength = 250, StopLoss = 0.06m, BuyThreshold = 0.12m, TakeProfitUp = 0.15m, TrailingDrop = 0.08m, MaxWeight = 0.12m } }
        };

        private Dictionary<Symbol, SymbolData> _symbolDataMap = new Dictionary<Symbol, SymbolData>();
        private DateTime _lastContributionDate = DateTime.MinValue;

        public override void Initialize()
        {
            SetStartDate(2018, 1, 1); // 覆盖最近一轮完整牛熊
            SetEndDate(2025, 1, 1);
            SetCash(100000);

            foreach (var ticker in _config.Keys)
            {
                var symbol = AddEquity(ticker, Resolution.Daily).Symbol;
                Securities[symbol].SetDataNormalizationMode(DataNormalizationMode.Raw);
                var sd = new SymbolData
                {
                    Symbol = symbol,
                    Settings = _config[ticker],
                    Sma = SMA(symbol, _config[ticker].SmaLength, Resolution.Daily)
                };

                // 预热均线数据
                var history = History<TradeBar>(symbol, sd.Settings.SmaLength, Resolution.Daily);
                foreach (var bar in history) sd.Sma.Update(bar.EndTime, bar.Close);
                _symbolDataMap.Add(symbol, sd);
            }
        }

        public override void OnData(Slice data)
        {
            // 每月定投逻辑
            if (Time.Date.Month != _lastContributionDate.Month)
            {
                Portfolio.CashBook["USD"].AddAmount(_monthlyContribution);
                _lastContributionDate = Time.Date;
            }

            foreach (var sd in _symbolDataMap.Values)
            {
                if (!data.Bars.TryGetValue(sd.Symbol, out TradeBar bar)) continue;
                if (!sd.Sma.IsReady) continue;

                sd.ActionToday = false;
                HandleSellLogic(sd, bar.Close);
                if (!sd.ActionToday) HandleBuyLogic(sd, bar.Close);
            }
        }

        private void HandleBuyLogic(SymbolData sd, decimal price)
        {
            decimal currentWeight = Portfolio[sd.Symbol].HoldingsValue / Portfolio.TotalPortfolioValue;
            decimal weightGap = sd.Settings.MaxWeight - currentWeight; // 使用独立权重上限

            if (weightGap < _rebalanceThreshold) return;

            // 均线回归买入逻辑：价格低于 (均线 * (1 - 阈值))
            decimal buyTrigger = sd.Sma.Current.Value * (1 - sd.Settings.BuyThreshold);
            if (price < buyTrigger)
            {
                decimal amountToSpend = Portfolio.TotalPortfolioValue * _buyStep;
                if (Portfolio.Cash < amountToSpend) amountToSpend = Portfolio.Cash;

                int qty = (int)(amountToSpend / price);
                if (qty > 0)
                {
                    MarketOrder(sd.Symbol, qty);
                    sd.ActionToday = true;
                    Debug($"{Time}: 买入 {sd.Symbol} @ {price}, 偏离度: {(price / sd.Sma.Current.Value - 1):P2}");
                }
            }
        }

        private void HandleSellLogic(SymbolData sd, decimal price)
        {
            foreach (var lot in sd.Lots.Where(l => !l.PendingSell).ToList())
            {
                if (price > lot.HighestPrice) lot.HighestPrice = price;

                // 1. 固定止损
                bool stopLoss = price <= lot.EntryPrice * (1 - sd.Settings.StopLoss);

                // 2. 激活移动止盈逻辑
                if (!lot.TrailingActive && price >= lot.EntryPrice * (1 + sd.Settings.TakeProfitUp))
                {
                    lot.TrailingActive = true;
                }

                // 3. 移动止损触发
                bool trailingStop = lot.TrailingActive && price <= lot.HighestPrice * (1 - sd.Settings.TrailingDrop);

                if (stopLoss || trailingStop)
                {
                    MarketOrder(sd.Symbol, -lot.Quantity);
                    lot.PendingSell = true;
                    sd.ActionToday = true;
                    string reason = stopLoss ? "固定止损" : "移动止盈";
                    Debug($"{Time}: 卖出 {sd.Symbol} ({reason}) @ {price}, 利润: {(price / lot.EntryPrice - 1):P2}");
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled) return;
            var sd = _symbolDataMap[orderEvent.Symbol];

            if (orderEvent.Quantity > 0)
            {
                sd.Lots.Add(new Lot { Quantity = (int)orderEvent.FillQuantity, EntryPrice = orderEvent.FillPrice, HighestPrice = orderEvent.FillPrice });
            }
            else
            {
                int qtyToRem = (int)Math.Abs(orderEvent.FillQuantity);
                foreach (var lot in sd.Lots.OrderBy(l => l.EntryPrice).ToList())
                {
                    if (qtyToRem <= 0) break;
                    int take = Math.Min(lot.Quantity, qtyToRem);
                    lot.Quantity -= take; qtyToRem -= take;
                }
                sd.Lots.RemoveAll(l => l.Quantity <= 0);
            }
        }
    }
}
