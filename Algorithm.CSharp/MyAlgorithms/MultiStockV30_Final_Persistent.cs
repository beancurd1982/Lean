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
using QuantConnect.Brokerages;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    // 版本 V31: 八星组合实盘终极修正版
    // 修复了负现金可能导致反向开仓的 Bug，增强了稳健性
    public class MultiStockV30_Final_Persistent : QCAlgorithm
    {
        private const string StateKey = "EightStar_State_V30_Key";

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
            public bool ActionToday = false;
        }

        public class Lot
        {
            public int Quantity { get; set; }
            public decimal EntryPrice { get; set; }
            public decimal HighestPrice { get; set; }
            public bool TrailingActive { get; set; }
            public bool PendingSell { get; set; }
        }

        [Parameter("rebalance-threshold")]
        private decimal _rebalanceThreshold = 0.02m;

        [Parameter("buy-step")]
        private decimal _buyStep = 0.05m;

        [Parameter("monthly-contribution")]
        private decimal _monthlyContribution = 1000m;

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
            SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);
            SetStartDate(2018, 1, 1);
            SetEndDate(2025, 1, 1);

            if (!LiveMode) SetCash(100000);

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

                var history = History<TradeBar>(symbol, sd.Settings.SmaLength, Resolution.Daily);
                foreach (var bar in history) sd.Sma.Update(bar.EndTime, bar.Close);
                _symbolDataMap.Add(symbol, sd);
            }

            if (LiveMode)
            {
                LoadState();

                // 孤儿持仓恢复逻辑
                foreach (var sd in _symbolDataMap.Values)
                {
                    var currentQty = Portfolio[sd.Symbol].Quantity;
                    // 如果有持仓但没有 Lot 记录，说明是“孤儿”持仓，需要认领
                    if (currentQty != 0 && sd.Lots.Count == 0)
                    {
                        var avgPrice = Portfolio[sd.Symbol].AveragePrice;
                        var currentPrice = Securities[sd.Symbol].Price;
                        sd.Lots.Add(new Lot
                        {
                            Quantity = (int)currentQty,
                            EntryPrice = avgPrice,
                            // 恢复时保守处理：假设最高价就是当前价或成本价的较高者
                            HighestPrice = Math.Max(avgPrice, currentPrice),
                            TrailingActive = false
                        });
                        Debug($"[系统恢复] {sd.Symbol}: 恢复孤儿持仓 {currentQty} 股。");
                    }
                }
            }
        }

        public override void OnData(Slice data)
        {
            if (Time.Date.Month != _lastContributionDate.Month)
            {
                if (!LiveMode) Portfolio.CashBook["USD"].AddAmount(_monthlyContribution);
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
            // 修复 1: 防止总资产为 0 导致的除零错误
            if (Portfolio.TotalPortfolioValue <= 0) return;

            decimal currentWeight = Portfolio[sd.Symbol].HoldingsValue / Portfolio.TotalPortfolioValue;
            decimal weightGap = sd.Settings.MaxWeight - currentWeight;

            if (weightGap < _rebalanceThreshold) return;

            decimal buyTrigger = sd.Sma.Current.Value * (1 - sd.Settings.BuyThreshold);
            if (price < buyTrigger)
            {
                decimal amountToSpend = Portfolio.TotalPortfolioValue * _buyStep;

                // 修复 2: 严格限制可用现金
                if (Portfolio.Cash < amountToSpend) amountToSpend = Portfolio.Cash;

                // 修复 3: 核心修复！如果现金为负或零，坚决不买，防止变成 Short (负数 MarketOrder)
                if (amountToSpend <= 0) return;

                int qty = (int)(amountToSpend / price);
                if (qty > 0)
                {
                    MarketOrder(sd.Symbol, qty);
                    sd.ActionToday = true;
                    Debug($"{Time}: 买入 {sd.Symbol} @ {price}, 偏离: {(price / sd.Sma.Current.Value - 1):P2}");
                }
            }
        }

        private void HandleSellLogic(SymbolData sd, decimal price)
        {
            // 使用 ToList() 创建副本进行迭代，因为我们可能修改 lot 的属性
            foreach (var lot in sd.Lots.Where(l => !l.PendingSell).ToList())
            {
                if (price > lot.HighestPrice) lot.HighestPrice = price;

                bool stopLoss = price <= lot.EntryPrice * (1 - sd.Settings.StopLoss);

                if (!lot.TrailingActive && price >= lot.EntryPrice * (1 + sd.Settings.TakeProfitUp))
                {
                    lot.TrailingActive = true;
                }

                bool trailingStop = lot.TrailingActive && price <= lot.HighestPrice * (1 - sd.Settings.TrailingDrop);

                if (stopLoss || trailingStop)
                {
                    MarketOrder(sd.Symbol, -lot.Quantity);
                    lot.PendingSell = true;
                    sd.ActionToday = true;
                    string reason = stopLoss ? "止损" : "止盈";
                    Debug($"{Time}: 卖出 {sd.Symbol} ({reason})");
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled) return;
            var sd = _symbolDataMap[orderEvent.Symbol];

            if (orderEvent.Quantity > 0) // 买入成交
            {
                sd.Lots.Add(new Lot
                {
                    Quantity = (int)orderEvent.FillQuantity,
                    EntryPrice = orderEvent.FillPrice,
                    HighestPrice = orderEvent.FillPrice,
                    TrailingActive = false,
                    PendingSell = false
                });
            }
            else // 卖出成交
            {
                int qtyToRem = (int)Math.Abs(orderEvent.FillQuantity);

                // 1. 优先移除标记为 PendingSell 的 (算法卖出)
                var pendingLots = sd.Lots.Where(l => l.PendingSell).ToList();
                foreach (var lot in pendingLots)
                {
                    if (qtyToRem <= 0) break;
                    int take = Math.Min(lot.Quantity, qtyToRem);
                    lot.Quantity -= take;
                    qtyToRem -= take;
                }

                // 2. 如果还有剩余 (可能是手动卖出)，则移除其他的
                if (qtyToRem > 0)
                {
                    foreach (var lot in sd.Lots.OrderBy(l => l.EntryPrice).ToList())
                    {
                        if (qtyToRem <= 0) break;
                        int take = Math.Min(lot.Quantity, qtyToRem);
                        lot.Quantity -= take; qtyToRem -= take;
                    }
                }

                sd.Lots.RemoveAll(l => l.Quantity <= 0);
            }

            // 成交后立即保存状态，最安全的做法
            if (LiveMode) SaveState();
        }

        // --- 云端持久化方法 ---
        private void SaveState()
        {
            try
            {
                var stateToSave = new Dictionary<string, List<Lot>>();
                foreach (var kvp in _symbolDataMap)
                {
                    if (kvp.Value.Lots.Count > 0) stateToSave[kvp.Key.Value] = kvp.Value.Lots;
                }
                ObjectStore.SaveJson(StateKey, stateToSave);
            }
            catch (Exception ex)
            {
                Error($"SaveState Failed: {ex.Message}");
            }
        }

        private void LoadState()
        {
            if (ObjectStore.ContainsKey(StateKey))
            {
                try
                {
                    var loadedState = ObjectStore.ReadJson<Dictionary<string, List<Lot>>>(StateKey);
                    if (loadedState != null)
                    {
                        foreach (var kvp in loadedState)
                        {
                            var targetSd = _symbolDataMap.Values.FirstOrDefault(sd => sd.Symbol.Value == kvp.Key);
                            if (targetSd != null) targetSd.Lots = kvp.Value;
                        }
                        Debug("云端状态加载成功。");
                    }
                }
                catch (Exception ex)
                {
                    Error($"LoadState Failed: {ex.Message}");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (LiveMode) SaveState();
        }
    }
}
