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
    // 版本 V29: 参数暴露定稿版 (Final Parameterized Version)
    // 逻辑：将全局风控杠杆暴露给 Optimization 引擎，方便进行最终的参数敏感度分析
    public class MultiStockV29_Final : QCAlgorithm
    {
        public class SymbolSettings
        {
            public int SmaLength; public decimal BuyThreshold; public decimal TakeProfitUp;
            public decimal StopLoss; public decimal TrailingDrop;
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

        // --- 暴露的全局优化参数 ---
        [Parameter("max-weight-per-stock")]
        private decimal _maxWeightPerStock = 0.16m;

        [Parameter("rebalance-threshold")]
        private decimal _rebalanceThreshold = 0.03m;

        [Parameter("buy-step")]
        private decimal _buyStep = 0.05m;

        [Parameter("monthly-contribution")]
        private decimal _monthlyContribution = 500m;

        private Dictionary<string, SymbolSettings> _config = new Dictionary<string, SymbolSettings> {
            { "MSFT", new SymbolSettings { SmaLength = 200, BuyThreshold = 0.04m, TakeProfitUp = 0.10m, StopLoss = 0.04m, TrailingDrop = 0.05m } },
            { "CVX",  new SymbolSettings { SmaLength = 150, BuyThreshold = 0.10m, TakeProfitUp = 0.05m, StopLoss = 0.08m, TrailingDrop = 0.02m } },
            { "DUK",  new SymbolSettings { SmaLength = 175, BuyThreshold = 0.05m, TakeProfitUp = 0.03m, StopLoss = 0.05m, TrailingDrop = 0.01m } },
            { "JNJ",  new SymbolSettings { SmaLength = 240, BuyThreshold = 0.04m, TakeProfitUp = 0.10m, StopLoss = 0.04m, TrailingDrop = 0.01m } },
            { "PG",   new SymbolSettings { SmaLength = 225, BuyThreshold = 0.02m, TakeProfitUp = 0.10m, StopLoss = 0.055m, TrailingDrop = 0.03m } }
        };

        private Dictionary<Symbol, SymbolData> _symbolDataMap = new Dictionary<Symbol, SymbolData>();
        private DateTime _lastContributionDate = DateTime.MinValue;

        public override void Initialize()
        {
            SetStartDate(2015, 1, 1);
            SetEndDate(2025, 1, 1);
            SetCash(50000);

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
        }

        public override void OnData(Slice data)
        {
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
            decimal weightGap = _maxWeightPerStock - currentWeight;

            // 核心调优逻辑：根据死区参数决定是否执行调仓
            if (weightGap < _rebalanceThreshold) return;

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
                    MarketOrder(sd.Symbol, -lot.Quantity);
                    lot.PendingSell = true;
                    sd.ActionToday = true;
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
