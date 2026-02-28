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
using Newtonsoft.Json;
using QuantConnect.Brokerages; // 必须引用，防止编译错误
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    // 版本 V32: 八星组合实盘稳定版 (增加每6h报告、SaveState日志、提前10分钟交易)
    public class MultiStockV32_Final_Stable : QCAlgorithm
    {
        private const string StateKey = "EightStar_State_V32_Key";

        public class SymbolSettings
        {
            public int SmaLength;
            public decimal BuyThreshold;
            public decimal TakeProfitUp;
            public decimal StopLoss;
            public decimal TrailingDrop;
            public decimal MaxWeight;
        }

        public class SymbolData
        {
            public Symbol Symbol;
            public SimpleMovingAverage Sma;
            public SymbolSettings Settings;
            public List<Lot> Lots = new List<Lot>();
        }

        public class Lot
        {
            public int Quantity { get; set; }
            public decimal EntryPrice { get; set; }
            public decimal HighestPrice { get; set; }
            public bool TrailingActive { get; set; }
            public bool PendingSell { get; set; }
            public int PendingSellOrderId { get; set; }
        }

        [Parameter("rebalance-threshold")]
        private decimal _rebalanceThreshold = 0.02m;

        [Parameter("buy-step")]
        private decimal _buyStep = 0.05m;

        // 回测优化方法 27/02/2026：
        // 先固定stop-loss-pct = 0.04, trailing-drop-pct = 0.03，然后优化 sma-length, buy-threshold-pct, take-profit-up-pct 这三个维度的参数，
        // 最后微调 stop-loss-pct 和 trailing-drop-pct
        private Dictionary<string, SymbolSettings> _config = new Dictionary<string, SymbolSettings> {
            { "MSFT", new SymbolSettings { SmaLength = 200, StopLoss = 0.04m, BuyThreshold = 0.04m, TakeProfitUp = 0.10m, TrailingDrop = 0.05m, MaxWeight = 0.14m } },
            { "CVX",  new SymbolSettings { SmaLength = 150, StopLoss = 0.08m, BuyThreshold = 0.10m, TakeProfitUp = 0.05m, TrailingDrop = 0.02m, MaxWeight = 0.12m } },
            { "DUK",  new SymbolSettings { SmaLength = 175, StopLoss = 0.05m, BuyThreshold = 0.05m, TakeProfitUp = 0.03m, TrailingDrop = 0.01m, MaxWeight = 0.12m } },
            { "JNJ",  new SymbolSettings { SmaLength = 240, StopLoss = 0.04m, BuyThreshold = 0.04m, TakeProfitUp = 0.10m, TrailingDrop = 0.01m, MaxWeight = 0.13m } },
            { "PG",   new SymbolSettings { SmaLength = 225, StopLoss = 0.05m, BuyThreshold = 0.02m, TakeProfitUp = 0.10m, TrailingDrop = 0.03m, MaxWeight = 0.13m } },
            { "JPM",  new SymbolSettings { SmaLength = 240, StopLoss = 0.05m, BuyThreshold = 0.02m, TakeProfitUp = 0.06m, TrailingDrop = 0.025m, MaxWeight = 0.13m } },
            { "NVDA", new SymbolSettings { SmaLength = 120, StopLoss = 0.10m, BuyThreshold = 0.08m, TakeProfitUp = 0.25m, TrailingDrop = 0.09m, MaxWeight = 0.11m } },
            { "AVGO", new SymbolSettings { SmaLength = 250, StopLoss = 0.06m, BuyThreshold = 0.12m, TakeProfitUp = 0.15m, TrailingDrop = 0.08m, MaxWeight = 0.12m } },

            // 27/02/2026 更新：增加 AMZN，参数较为激进，适合高风险偏好投资者
            { "AMZN", new SymbolSettings { SmaLength = 180, StopLoss = 0.05m, BuyThreshold = 0.06m, TakeProfitUp = 0.10m, TrailingDrop = 0.06m, MaxWeight = 0.10m } }
        };

        private Dictionary<Symbol, SymbolData> _symbolDataMap = new Dictionary<Symbol, SymbolData>();

        public override void Initialize()
        {
            Debug(">>> [1/5] 初始化：设置经纪商模型 (IB Margin)...");
            SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);

            if (!LiveMode)
            {
                SetStartDate(2024, 1, 1);
                SetCash(250000);
            }

            Debug($">>> [2/5] 标的加载：正在预热 {_config.Count} 只股票数据...");
            foreach (var ticker in _config.Keys)
            {
                var symbol = AddEquity(ticker, Resolution.Daily).Symbol;
                Securities[symbol].SetDataNormalizationMode(DataNormalizationMode.Adjusted);
                var sd = new SymbolData
                {
                    Symbol = symbol,
                    Settings = _config[ticker],
                    Sma = SMA(symbol, _config[ticker].SmaLength, Resolution.Daily)
                };

                var history = History<TradeBar>(symbol, sd.Settings.SmaLength, Resolution.Daily);
                foreach (var bar in history) sd.Sma.Update(bar.EndTime, bar.Close);

                _symbolDataMap.Add(symbol, sd);
                Debug($"    - {ticker} 预热完成。SMA: {sd.Sma.Current.Value:N2}");
            }

            if (LiveMode)
            {
                Debug(">>> [3/5] 持久化：从 ObjectStore 恢复状态...");
                LoadState();

                Debug(">>> [4/5] 资产对账：检查现有仓位...");
                foreach (var sd in _symbolDataMap.Values)
                {
                    var currentQty = Portfolio[sd.Symbol].Quantity;
                    if (currentQty != 0 && sd.Lots.Count == 0)
                    {
                        sd.Lots.Add(new Lot
                        {
                            Quantity = (int)currentQty,
                            EntryPrice = Portfolio[sd.Symbol].AveragePrice,
                            HighestPrice = Math.Max(Portfolio[sd.Symbol].AveragePrice, Securities[sd.Symbol].Price),
                            TrailingActive = false
                        });
                        Debug($"    [RECOVERY] {sd.Symbol}: 发现并同步现有仓位。");
                    }
                }
            }

            // 定时器 A：每天 15:50 执行核心逻辑（提前10分钟，避开收盘拒单）
            Schedule.On(DateRules.EveryDay(), TimeRules.At(15, 50), ScanMarketLogic);

            // 定时器 B：每 6 小时打印一次账户报告
            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromHours(6)), PrintAccountReport);

            Debug(">>> [5/5] 初始化完毕！算法已进入 15:50 预交易监听模式。");
        }

        private void ScanMarketLogic()
        {
            Debug($">>> [{Time}] 启动每日交易检查...");
            foreach (var sd in _symbolDataMap.Values)
            {
                if (!sd.Sma.IsReady) continue;
                decimal price = Securities[sd.Symbol].Price;
                if (price <= 0) continue;

                // 卖出逻辑在前，买入逻辑在后
                HandleSellLogic(sd, price);
                HandleBuyLogic(sd, price);
            }
        }

        private void PrintAccountReport()
        {
            decimal unrealizedPnl = Portfolio.TotalUnrealizedProfit;
            Debug($"[REPORT] {Time} | 总资产: {Portfolio.TotalPortfolioValue:N2} | 现金: {Portfolio.Cash:N2} | 浮盈: {unrealizedPnl:N2}");
            foreach (var sd in _symbolDataMap.Values.Where(x => Portfolio[x.Symbol].Invested))
            {
                Debug($"    - 持仓: {sd.Symbol.Value} | 数量: {Portfolio[sd.Symbol].Quantity} | 现价: {Securities[sd.Symbol].Price:N2}");
            }
        }


        private void HandleBuyLogic(SymbolData sd, decimal price)
        {
            decimal totalValue = Portfolio.TotalPortfolioValue;
            if (totalValue <= 0) return;

            decimal holdingsValue = Portfolio[sd.Symbol].HoldingsValue;
            decimal maxAdditionalValue = (sd.Settings.MaxWeight * totalValue) - holdingsValue;
            if (maxAdditionalValue <= 0) return;

            decimal currentWeight = holdingsValue / totalValue;
            if (sd.Settings.MaxWeight - currentWeight < _rebalanceThreshold) return;

            decimal buyTrigger = sd.Sma.Current.Value * (1 - sd.Settings.BuyThreshold);
            if (price < buyTrigger)
            {
                decimal amountToSpend = Math.Min(Portfolio.Cash, Math.Min(totalValue * _buyStep, maxAdditionalValue));
                int qty = (int)(amountToSpend / price);
                if (qty > 0)
                {
                    MarketOrder(sd.Symbol, qty);
                    Debug($"[TRADE] {Time} Buy: {sd.Symbol} @ {price}");
                }
            }
        }

        private void HandleSellLogic(SymbolData sd, decimal price)
        {
            foreach (var lot in sd.Lots.Where(l => !l.PendingSell).ToList())
            {
                if (price > lot.HighestPrice) lot.HighestPrice = price;

                bool stopLoss = price <= lot.EntryPrice * (1 - sd.Settings.StopLoss);
                if (!lot.TrailingActive && price >= lot.EntryPrice * (1 + sd.Settings.TakeProfitUp)) lot.TrailingActive = true;
                bool trailingStop = lot.TrailingActive && price <= lot.HighestPrice * (1 - sd.Settings.TrailingDrop);

                if (stopLoss || trailingStop)
                {
                    var ticket = MarketOrder(sd.Symbol, -lot.Quantity);
                    if (ticket.OrderId <= 0 || ticket.Status == OrderStatus.Invalid)
                    {
                        Error($"[TRADE] {Time} Sell order failed: {sd.Symbol} ({(stopLoss ? "StopLoss" : "TrailingStop")}) @ {price}");
                        continue;
                    }

                    lot.PendingSell = true;
                    lot.PendingSellOrderId = ticket.OrderId;
                    Debug($"[TRADE] {Time} 发起卖出: {sd.Symbol} ({(stopLoss ? "止损" : "移动止盈")}) @ {price}");
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (!_symbolDataMap.TryGetValue(orderEvent.Symbol, out var sd)) return;

            bool stateChanged = false;
            bool isSell = orderEvent.Direction == OrderDirection.Sell || orderEvent.FillQuantity < 0 || orderEvent.Quantity < 0;

            if (orderEvent.FillQuantity > 0)
            {
                sd.Lots.Add(new Lot { Quantity = (int)orderEvent.FillQuantity, EntryPrice = orderEvent.FillPrice, HighestPrice = orderEvent.FillPrice });
                stateChanged = true;
            }
            else if (orderEvent.FillQuantity < 0)
            {
                int qtyToRem = (int)Math.Abs(orderEvent.FillQuantity);
                var lot = sd.Lots.FirstOrDefault(l => l.PendingSellOrderId == orderEvent.OrderId);

                if (lot == null)
                {
                    var pendingLots = sd.Lots.Where(l => l.PendingSell).ToList();
                    if (pendingLots.Count == 1)
                    {
                        lot = pendingLots[0];
                        Debug($"[WARN] {Time} Sell fill without matching order id. Using only pending lot. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId}");
                    }
                    else
                    {
                        Error($"[WARN] {Time} Sell fill without matching lot. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId} PendingLots={pendingLots.Count}");
                    }
                }

                if (lot != null)
                {
                    int take = Math.Min(lot.Quantity, qtyToRem);
                    lot.Quantity -= take;
                    qtyToRem -= take;

                    if (qtyToRem > 0)
                    {
                        Error($"[WARN] {Time} Sell fill exceeds tracked lot quantity. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId} Remaining={qtyToRem}");
                    }

                    sd.Lots.RemoveAll(l => l.Quantity <= 0);
                    stateChanged = true;
                }
            }

            if (isSell && (orderEvent.Status == OrderStatus.Filled || orderEvent.Status == OrderStatus.Canceled || orderEvent.Status == OrderStatus.Invalid))
            {
                var lot = sd.Lots.FirstOrDefault(l => l.PendingSellOrderId == orderEvent.OrderId);

                if (lot == null)
                {
                    var pendingLots = sd.Lots.Where(l => l.PendingSell).ToList();
                    if (pendingLots.Count == 1)
                    {
                        lot = pendingLots[0];
                        Debug($"[WARN] {Time} Sell order resolved without matching order id. Clearing only pending lot. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId}");
                    }
                }

                if (lot != null)
                {
                    lot.PendingSell = false;
                    lot.PendingSellOrderId = 0;
                    stateChanged = true;
                }
                else
                {
                    Error($"[WARN] {Time} Sell order resolved without matching lot. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId}");
                }
            }

            if (stateChanged && LiveMode) SaveState();
        }

        private void SaveState()
        {
            try
            {
                var state = _symbolDataMap.Where(kvp => kvp.Value.Lots.Any()).ToDictionary(kvp => kvp.Key.Value, kvp => kvp.Value.Lots);
                ObjectStore.SaveJson(StateKey, state);
                // 满足你的要求：增加详细保存日志
                Debug($"[CloudSave] {Time}: 状态已备份。当前管理持仓股票: {string.Join(", ", state.Keys)}");
            }
            catch (Exception ex) { Error($"Save Error: {ex.Message}"); }
        }

        private void LoadState()
        {
            if (!ObjectStore.ContainsKey(StateKey)) return;
            try
            {
                var loaded = ObjectStore.ReadJson<Dictionary<string, List<Lot>>>(StateKey);
                foreach (var kvp in loaded)
                {
                    var sd = _symbolDataMap.Values.FirstOrDefault(s => s.Symbol.Value == kvp.Key);
                    if (sd != null) sd.Lots = kvp.Value;
                }
                Debug($"[CloudLoad] {Time}: 成功恢复 {loaded.Count} 个标的的持仓详细记录。");
            }
            catch (Exception ex) { Error($"Load Error: {ex.Message}"); }
        }

        public override void OnEndOfAlgorithm() { if (LiveMode) SaveState(); }
    }
}
