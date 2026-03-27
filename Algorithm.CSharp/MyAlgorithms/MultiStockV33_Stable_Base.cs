#region imports
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Parameters;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    // V33 stable base: cleanup of V32 with behavior-preserving structure and clearer logs.
    public class MultiStockV33_Stable_Base : QCAlgorithm
    {
        // Preserve the existing key so live deployments can continue loading V32 lot state.
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

        // Minimum remaining headroom to MaxWeight required before another buy is allowed.
        [Parameter("rebalance-threshold")]
        private decimal _rebalanceThreshold = 0.01m;

        // Target spend per buy attempt as a fraction of total portfolio value.
        [Parameter("buy-step")]
        private decimal _buyStep = 0.05m;

        // Current optimized configuration as of 2026-02-27.
        private readonly Dictionary<string, SymbolSettings> _config = new Dictionary<string, SymbolSettings>
        {
            { "MSFT", new SymbolSettings { SmaLength = 200, StopLoss = 0.04m, BuyThreshold = 0.04m, TakeProfitUp = 0.10m, TrailingDrop = 0.05m, MaxWeight = 0.12m } },
            { "CVX",  new SymbolSettings { SmaLength = 150, StopLoss = 0.08m, BuyThreshold = 0.10m, TakeProfitUp = 0.05m, TrailingDrop = 0.02m, MaxWeight = 0.10m } },
            { "DUK",  new SymbolSettings { SmaLength = 175, StopLoss = 0.05m, BuyThreshold = 0.05m, TakeProfitUp = 0.03m, TrailingDrop = 0.01m, MaxWeight = 0.10m } },
            { "JNJ",  new SymbolSettings { SmaLength = 240, StopLoss = 0.04m, BuyThreshold = 0.04m, TakeProfitUp = 0.10m, TrailingDrop = 0.01m, MaxWeight = 0.11m } },
            { "PG",   new SymbolSettings { SmaLength = 225, StopLoss = 0.05m, BuyThreshold = 0.02m, TakeProfitUp = 0.10m, TrailingDrop = 0.03m, MaxWeight = 0.10m } },
            { "JPM",  new SymbolSettings { SmaLength = 240, StopLoss = 0.05m, BuyThreshold = 0.02m, TakeProfitUp = 0.06m, TrailingDrop = 0.025m, MaxWeight = 0.10m } },
            { "NVDA", new SymbolSettings { SmaLength = 120, StopLoss = 0.10m, BuyThreshold = 0.08m, TakeProfitUp = 0.25m, TrailingDrop = 0.09m, MaxWeight = 0.08m } },
            { "AVGO", new SymbolSettings { SmaLength = 250, StopLoss = 0.06m, BuyThreshold = 0.12m, TakeProfitUp = 0.15m, TrailingDrop = 0.08m, MaxWeight = 0.09m } },
            { "AMZN", new SymbolSettings { SmaLength = 180, StopLoss = 0.05m, BuyThreshold = 0.06m, TakeProfitUp = 0.10m, TrailingDrop = 0.06m, MaxWeight = 0.10m } },
            { "COST", new SymbolSettings { SmaLength = 180, StopLoss = 0.04m, BuyThreshold = 0.06m, TakeProfitUp = 0.18m, TrailingDrop = 0.06m, MaxWeight = 0.10m } },
            { "UNH",  new SymbolSettings { SmaLength = 180, StopLoss = 0.03m, BuyThreshold = 0.03m, TakeProfitUp = 0.14m, TrailingDrop = 0.04m, MaxWeight = 0.10m } },
            { "WMT",  new SymbolSettings { SmaLength = 220, StopLoss = 0.08m, BuyThreshold = 0.04m, TakeProfitUp = 0.14m, TrailingDrop = 0.02m, MaxWeight = 0.10m } },
        };

        private readonly Dictionary<Symbol, SymbolData> _symbolDataMap = new Dictionary<Symbol, SymbolData>();

        public override void Initialize()
        {
            Debug(">>> [1/5] Initialize: set brokerage model (IB margin).");
            SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);

            if (!LiveMode)
            {
                SetStartDate(2015, 1, 1);
                SetEndDate(2025, 1, 1);
                SetCash(250000);
            }

            InitializeSymbols();

            if (LiveMode)
            {
                Debug(">>> [3/5] Restore persisted lot state from ObjectStore.");
                LoadState();

                Debug(">>> [4/5] Reconcile existing live holdings.");
                RecoverLiveHoldings();
            }

            InitializeSchedules();

            Debug(">>> [5/5] Initialization complete. Daily scan scheduled for 15:50.");
        }

        private void InitializeSymbols()
        {
            Debug($">>> [2/5] Load and warm up {_config.Count} symbols.");

            foreach (var ticker in _config.Keys)
            {
                var symbol = AddEquity(ticker, Resolution.Daily).Symbol;
                Securities[symbol].SetDataNormalizationMode(DataNormalizationMode.Adjusted);

                var settings = _config[ticker];
                var symbolData = new SymbolData
                {
                    Symbol = symbol,
                    Settings = settings,
                    Sma = SMA(symbol, settings.SmaLength, Resolution.Daily)
                };

                var history = History<TradeBar>(symbol, settings.SmaLength, Resolution.Daily);
                foreach (var bar in history)
                {
                    symbolData.Sma.Update(bar.EndTime, bar.Close);
                }

                _symbolDataMap.Add(symbol, symbolData);
                Debug($"    - {ticker} warmup complete. SMA: {symbolData.Sma.Current.Value:N2}");
            }
        }

        private void RecoverLiveHoldings()
        {
            foreach (var symbolData in _symbolDataMap.Values)
            {
                var currentQuantity = Portfolio[symbolData.Symbol].Quantity;
                if (currentQuantity != 0 && symbolData.Lots.Count == 0)
                {
                    symbolData.Lots.Add(new Lot
                    {
                        Quantity = (int)currentQuantity,
                        EntryPrice = Portfolio[symbolData.Symbol].AveragePrice,
                        HighestPrice = Math.Max(Portfolio[symbolData.Symbol].AveragePrice, Securities[symbolData.Symbol].Price),
                        TrailingActive = false
                    });

                    Debug($"    [RECOVERY] {symbolData.Symbol}: recovered existing holding into tracked lots.");
                }
            }
        }

        private void InitializeSchedules()
        {
            var scheduleTicker = _config.Keys.First();

            // Use one symbol's trading calendar so schedules skip weekends and market holidays.
            Schedule.On(DateRules.EveryDay(scheduleTicker), TimeRules.At(15, 50), ScanMarketLogic);
            Schedule.On(DateRules.EveryDay(scheduleTicker), TimeRules.Every(TimeSpan.FromHours(6)), PrintAccountReport);
        }

        private void ScanMarketLogic()
        {
            Debug($">>> [{Time}] Start daily trade scan.");

            foreach (var symbolData in _symbolDataMap.Values)
            {
                if (!symbolData.Sma.IsReady)
                {
                    continue;
                }

                if (!Securities[symbolData.Symbol].Exchange.ExchangeOpen)
                {
                    continue;
                }

                var price = Securities[symbolData.Symbol].Price;
                if (price <= 0)
                {
                    continue;
                }

                // Process exits before entries.
                HandleSellLogic(symbolData, price);
                HandleBuyLogic(symbolData, price);
            }
        }

        private void PrintAccountReport()
        {
            var unrealizedPnl = Portfolio.TotalUnrealizedProfit;
            Debug($"[REPORT] {Time} | Total Value: {Portfolio.TotalPortfolioValue:N2} | Cash: {Portfolio.Cash:N2} | Unrealized PnL: {unrealizedPnl:N2}");

            foreach (var symbolData in _symbolDataMap.Values.Where(x => Portfolio[x.Symbol].Invested))
            {
                Debug($"    - Holding: {symbolData.Symbol.Value} | Quantity: {Portfolio[symbolData.Symbol].Quantity} | Price: {Securities[symbolData.Symbol].Price:N2}");
            }
        }

        private void HandleBuyLogic(SymbolData symbolData, decimal price)
        {
            var totalValue = Portfolio.TotalPortfolioValue;
            if (totalValue <= 0)
            {
                return;
            }

            var holdingsValue = Portfolio[symbolData.Symbol].HoldingsValue;
            var maxAdditionalValue = (symbolData.Settings.MaxWeight * totalValue) - holdingsValue;
            if (maxAdditionalValue <= 0)
            {
                return;
            }

            var currentWeight = holdingsValue / totalValue;
            if (symbolData.Settings.MaxWeight - currentWeight < _rebalanceThreshold)
            {
                return;
            }

            var buyTrigger = symbolData.Sma.Current.Value * (1 - symbolData.Settings.BuyThreshold);
            if (price < buyTrigger)
            {
                var amountToSpend = Math.Min(Portfolio.Cash, Math.Min(totalValue * _buyStep, maxAdditionalValue));
                var quantity = (int)(amountToSpend / price);
                if (quantity > 0)
                {
                    MarketOrder(symbolData.Symbol, quantity);
                    Debug($"[TRADE] {Time} Buy: {symbolData.Symbol} @ {price}");
                }
            }
        }

        private void HandleSellLogic(SymbolData symbolData, decimal price)
        {
            foreach (var lot in symbolData.Lots.Where(l => !l.PendingSell).ToList())
            {
                if (price > lot.HighestPrice)
                {
                    lot.HighestPrice = price;
                }

                var stopLoss = price <= lot.EntryPrice * (1 - symbolData.Settings.StopLoss);
                if (!lot.TrailingActive && price >= lot.EntryPrice * (1 + symbolData.Settings.TakeProfitUp))
                {
                    lot.TrailingActive = true;
                }

                var trailingStop = lot.TrailingActive && price <= lot.HighestPrice * (1 - symbolData.Settings.TrailingDrop);

                if (!stopLoss && !trailingStop)
                {
                    continue;
                }

                // Mark pending before submission; async submit avoids immediate fill race in backtests.
                lot.PendingSell = true;
                var ticket = MarketOrder(symbolData.Symbol, -lot.Quantity, true);
                if (ticket.OrderId <= 0 || ticket.Status == OrderStatus.Invalid)
                {
                    lot.PendingSell = false;
                    lot.PendingSellOrderId = 0;
                    Error($"[TRADE] {Time} Sell order failed: {symbolData.Symbol} ({(stopLoss ? "StopLoss" : "TrailingStop")}) @ {price}");
                    continue;
                }

                lot.PendingSellOrderId = ticket.OrderId;
                Debug($"[TRADE] {Time} Submit sell: {symbolData.Symbol} ({(stopLoss ? "StopLoss" : "TrailingStop")}) @ {price}");
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (!_symbolDataMap.TryGetValue(orderEvent.Symbol, out var symbolData))
            {
                return;
            }

            var stateChanged = false;
            var isSell = orderEvent.Direction == OrderDirection.Sell || orderEvent.FillQuantity < 0 || orderEvent.Quantity < 0;

            if (orderEvent.FillQuantity > 0)
            {
                symbolData.Lots.Add(new Lot
                {
                    Quantity = (int)orderEvent.FillQuantity,
                    EntryPrice = orderEvent.FillPrice,
                    HighestPrice = orderEvent.FillPrice
                });
                stateChanged = true;
            }
            else if (orderEvent.FillQuantity < 0)
            {
                stateChanged = HandleSellFill(symbolData, orderEvent) || stateChanged;
            }

            if (isSell && IsResolvedSellStatus(orderEvent.Status))
            {
                stateChanged = ResolvePendingSell(symbolData, orderEvent) || stateChanged;
            }

            if (stateChanged && LiveMode)
            {
                SaveState();
            }
        }

        private bool HandleSellFill(SymbolData symbolData, OrderEvent orderEvent)
        {
            var quantityToRemove = (int)Math.Abs(orderEvent.FillQuantity);
            var lot = FindLotForSellFill(symbolData, orderEvent);
            if (lot == null)
            {
                return false;
            }

            var quantityTaken = Math.Min(lot.Quantity, quantityToRemove);
            lot.Quantity -= quantityTaken;
            quantityToRemove -= quantityTaken;

            if (quantityToRemove > 0)
            {
                Debug($"[WARN] {Time} Sell fill exceeds tracked lot quantity. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId} Remaining={quantityToRemove}");
            }

            symbolData.Lots.RemoveAll(l => l.Quantity <= 0);
            return true;
        }

        private Lot FindLotForSellFill(SymbolData symbolData, OrderEvent orderEvent)
        {
            var lot = symbolData.Lots.FirstOrDefault(l => l.PendingSellOrderId == orderEvent.OrderId);
            if (lot != null)
            {
                return lot;
            }

            var pendingLots = symbolData.Lots.Where(l => l.PendingSell).ToList();
            if (pendingLots.Count == 1)
            {
                Debug($"[WARN] {Time} Sell fill without matching order id. Using only pending lot. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId}");
                return pendingLots[0];
            }

            if (pendingLots.Count > 1)
            {
                Debug($"[WARN] {Time} Sell fill without matching order id. PendingLots={pendingLots.Count}; using first pending lot. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId}");
                return pendingLots.OrderBy(l => l.EntryPrice).First();
            }

            lot = symbolData.Lots.OrderBy(l => l.EntryPrice).FirstOrDefault();
            if (lot != null)
            {
                Debug($"[WARN] {Time} Sell fill without pending lot. Using first tracked lot. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId}");
            }
            else
            {
                Debug($"[WARN] {Time} Sell fill without tracked lot. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId}");
            }

            return lot;
        }

        private static bool IsResolvedSellStatus(OrderStatus status)
        {
            return status == OrderStatus.Filled
                || status == OrderStatus.Canceled
                || status == OrderStatus.Invalid;
        }

        private bool ResolvePendingSell(SymbolData symbolData, OrderEvent orderEvent)
        {
            var lot = symbolData.Lots.FirstOrDefault(l => l.PendingSellOrderId == orderEvent.OrderId);
            if (lot == null)
            {
                var pendingLots = symbolData.Lots.Where(l => l.PendingSell).ToList();
                if (pendingLots.Count > 0)
                {
                    lot = pendingLots.OrderBy(l => l.EntryPrice).First();
                    Debug($"[WARN] {Time} Sell order resolved without matching order id. PendingLots={pendingLots.Count}; clearing first pending lot. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId}");
                }
            }

            if (lot == null)
            {
                Debug($"[WARN] {Time} Sell order resolved without matching lot. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId}");
                return false;
            }

            lot.PendingSell = false;
            lot.PendingSellOrderId = 0;
            return true;
        }

        private void SaveState()
        {
            try
            {
                var state = _symbolDataMap
                    .Where(kvp => kvp.Value.Lots.Any())
                    .ToDictionary(kvp => kvp.Key.Value, kvp => kvp.Value.Lots);

                ObjectStore.SaveJson(StateKey, state);
                Debug($"[CloudSave] {Time}: state saved. Symbols tracked: {string.Join(", ", state.Keys)}");
            }
            catch (Exception ex)
            {
                Error($"Save Error: {ex.Message}");
            }
        }

        private void LoadState()
        {
            if (!ObjectStore.ContainsKey(StateKey))
            {
                return;
            }

            try
            {
                var loaded = ObjectStore.ReadJson<Dictionary<string, List<Lot>>>(StateKey);
                foreach (var kvp in loaded)
                {
                    var symbolData = _symbolDataMap.Values.FirstOrDefault(s => s.Symbol.Value == kvp.Key);
                    if (symbolData != null)
                    {
                        symbolData.Lots = kvp.Value;
                    }
                }

                Debug($"[CloudLoad] {Time}: restored lot details for {loaded.Count} symbols.");
            }
            catch (Exception ex)
            {
                Error($"Load Error: {ex.Message}");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (LiveMode)
            {
                SaveState();
            }
        }
    }
}
