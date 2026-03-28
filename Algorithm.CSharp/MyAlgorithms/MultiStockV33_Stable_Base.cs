#region imports
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        // V33 starts from a clean persistence namespace after paper-account reset.
        private const string StateKey = "EightStar_State_V33_Key";

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
            public bool TradingBlocked;
            public string BlockedReason;
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
        private string _lastSavedStateFingerprint;

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

            ValidateConfiguration();
            InitializeSymbols();

            if (LiveMode)
            {
                Debug(">>> [3/5] Restore persisted lot state from ObjectStore.");
                LoadState();

                Debug(">>> [4/5] Reconcile tracked lots with live holdings and open orders.");
                ReconcileLiveState();
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

            Debug($"[CONFIG] {Time} Symbols initialized. Count={_symbolDataMap.Count} TotalMaxWeight={_config.Values.Sum(x => x.MaxWeight):N2} BuyStep={_buyStep:N3} RebalanceThreshold={_rebalanceThreshold:N3}");
        }

        private void ReconcileLiveState()
        {
            var rebuiltSymbols = 0;
            var blockedSymbols = 0;
            var activeBlockedSymbols = new List<string>();

            foreach (var symbolData in _symbolDataMap.Values)
            {
                var lotCountBefore = symbolData.Lots.Count;
                var pendingCountBefore = symbolData.Lots.Count(lot => lot.PendingSell);
                var wasBlocked = symbolData.TradingBlocked;

                var isReady = EnsureSymbolStateReady(symbolData, "Startup reconciliation");
                var lotCountAfter = symbolData.Lots.Count;
                var pendingCountAfter = symbolData.Lots.Count(lot => lot.PendingSell);

                if (!wasBlocked && symbolData.TradingBlocked)
                {
                    blockedSymbols++;
                }

                if (isReady && (lotCountBefore != lotCountAfter || pendingCountBefore != pendingCountAfter))
                {
                    rebuiltSymbols++;
                }

                if (symbolData.TradingBlocked)
                {
                    activeBlockedSymbols.Add(symbolData.Symbol.Value);
                }
            }

            Debug($"[STARTUP] {Time} Reconciliation complete. Symbols={_symbolDataMap.Count} Rebuilt={rebuiltSymbols} NewlyBlocked={blockedSymbols} ActiveBlocked={activeBlockedSymbols.Count}");
            if (activeBlockedSymbols.Any())
            {
                Error($"[STARTUP] {Time} Blocked symbols after reconciliation: {string.Join(", ", activeBlockedSymbols)}");
            }
        }

        private List<OrderTicket> GetOpenSellTickets(Symbol symbol)
        {
            return Transactions.GetOpenOrders(symbol)
                .Where(order => order.Direction == OrderDirection.Sell)
                .Select(order => Transactions.GetOrderTicket(order.Id))
                .Where(ticket => ticket != null)
                .OrderBy(ticket => ticket.OrderId)
                .ToList();
        }

        private bool EnsureSymbolStateReady(SymbolData symbolData, string context)
        {
            var currentQuantity = (int)Portfolio[symbolData.Symbol].Quantity;
            var openSellTickets = GetOpenSellTickets(symbolData.Symbol);

            if (!TryGetInvariantViolation(symbolData, currentQuantity, openSellTickets, out _))
            {
                if (symbolData.TradingBlocked)
                {
                    symbolData.TradingBlocked = false;
                    symbolData.BlockedReason = null;
                    Debug($"[RECOVERY] {Time} Symbol state recovered and trading resumed. Symbol={symbolData.Symbol} Context={context}");
                }

                return true;
            }

            return TryRecoverOrBlockSymbolState(symbolData, openSellTickets, currentQuantity, context);
        }

        private static bool TryGetInvariantViolation(SymbolData symbolData, int currentQuantity, List<OrderTicket> openSellTickets, out string violation)
        {
            violation = null;

            if (currentQuantity < 0)
            {
                violation = $"Broker holdings are short ({currentQuantity}).";
                return true;
            }

            if (symbolData.Lots.Any(lot => lot.Quantity <= 0))
            {
                violation = "Tracked lot quantity must be positive.";
                return true;
            }

            if (symbolData.Lots.Any(lot => lot.EntryPrice <= 0))
            {
                violation = "Tracked lot entry price must be positive.";
                return true;
            }

            if (symbolData.Lots.Any(lot => lot.HighestPrice <= 0))
            {
                violation = "Tracked lot highest price must be positive.";
                return true;
            }

            if (symbolData.Lots.Any(lot => lot.HighestPrice < lot.EntryPrice))
            {
                violation = "Tracked lot highest price cannot be below entry price.";
                return true;
            }

            if (symbolData.Lots.Any(lot => !lot.PendingSell && lot.PendingSellOrderId != 0))
            {
                violation = "Non-pending lot has a pending sell order id.";
                return true;
            }

            if (symbolData.Lots.Any(lot => lot.PendingSell && lot.PendingSellOrderId <= 0))
            {
                violation = "Pending sell lot is missing a valid order id.";
                return true;
            }

            var pendingOrderIds = symbolData.Lots
                .Where(lot => lot.PendingSell)
                .Select(lot => lot.PendingSellOrderId)
                .ToList();

            if (pendingOrderIds.Count != pendingOrderIds.Distinct().Count())
            {
                violation = "Pending sell order ids must be unique per lot.";
                return true;
            }

            var trackedQuantity = symbolData.Lots.Sum(lot => lot.Quantity);
            if (trackedQuantity != currentQuantity)
            {
                violation = $"Tracked quantity ({trackedQuantity}) does not match broker holdings ({currentQuantity}).";
                return true;
            }

            var openOrderIds = openSellTickets
                .Select(ticket => ticket.OrderId)
                .ToHashSet();

            if (!pendingOrderIds.ToHashSet().SetEquals(openOrderIds))
            {
                violation = "Tracked pending sell order ids do not match broker open sell orders.";
                return true;
            }

            var openSellQuantity = openSellTickets.Sum(ticket => (int)Math.Abs(ticket.QuantityRemaining));
            if (openSellQuantity > currentQuantity)
            {
                violation = $"Open sell quantity ({openSellQuantity}) exceeds broker holdings ({currentQuantity}).";
                return true;
            }

            return false;
        }

        private bool TryRecoverOrBlockSymbolState(SymbolData symbolData, List<OrderTicket> openSellTickets, int currentQuantity, string context)
        {
            TryGetInvariantViolation(symbolData, currentQuantity, openSellTickets, out var violation);
            var reason = $"{context}. {violation}";

            if (TryRebuildTrackedLotsFromBrokerState(symbolData, openSellTickets, currentQuantity, reason))
            {
                if (symbolData.TradingBlocked)
                {
                    symbolData.TradingBlocked = false;
                    symbolData.BlockedReason = null;
                    Debug($"[RECOVERY] {Time} Symbol rebuilt from broker state and trading resumed. Symbol={symbolData.Symbol} Context={context}");
                }

                return true;
            }

            BlockSymbol(symbolData, reason);
            return false;
        }

        private bool TryRebuildTrackedLotsFromBrokerState(SymbolData symbolData, string reason)
        {
            var currentQuantity = (int)Portfolio[symbolData.Symbol].Quantity;
            var openSellTickets = GetOpenSellTickets(symbolData.Symbol);
            return TryRebuildTrackedLotsFromBrokerState(symbolData, openSellTickets, currentQuantity, reason);
        }

        private bool TryRebuildTrackedLotsFromBrokerState(SymbolData symbolData, List<OrderTicket> openSellTickets, int currentQuantity, string reason)
        {
            var previousLotCount = symbolData.Lots.Count;
            var previousPendingCount = symbolData.Lots.Count(lot => lot.PendingSell);
            var previousTrackedQuantity = symbolData.Lots.Sum(lot => lot.Quantity);

            if (currentQuantity < 0)
            {
                Error($"[REBUILD] {Time} Cannot rebuild symbol with short broker holdings. Symbol={symbolData.Symbol} Quantity={currentQuantity} Reason={reason}");
                return false;
            }

            var marketPrice = Securities[symbolData.Symbol].Price;
            var referencePrice = Portfolio[symbolData.Symbol].AveragePrice;
            if (referencePrice <= 0)
            {
                referencePrice = marketPrice;
            }

            if (currentQuantity > 0 && referencePrice <= 0)
            {
                Error($"[REBUILD] {Time} Cannot rebuild symbol without a valid reference price. Symbol={symbolData.Symbol} Holdings={currentQuantity} MarketPrice={marketPrice} AveragePrice={Portfolio[symbolData.Symbol].AveragePrice} Reason={reason}");
                return false;
            }

            var highestPrice = Math.Max(referencePrice, marketPrice);
            var rebuiltLots = new List<Lot>();
            var remainingQuantity = currentQuantity;

            foreach (var ticket in openSellTickets)
            {
                var pendingQuantity = (int)Math.Abs(ticket.QuantityRemaining);
                if (pendingQuantity <= 0)
                {
                    Error($"[REBUILD] {Time} Cannot rebuild symbol with non-positive open sell quantity. Symbol={symbolData.Symbol} OrderId={ticket.OrderId} QuantityRemaining={ticket.QuantityRemaining} Reason={reason}");
                    return false;
                }

                if (pendingQuantity > remainingQuantity)
                {
                    Error($"[REBUILD] {Time} Cannot rebuild symbol because open sell orders exceed broker holdings. Symbol={symbolData.Symbol} OrderId={ticket.OrderId} QuantityRemaining={ticket.QuantityRemaining} Holdings={currentQuantity} Reason={reason}");
                    return false;
                }

                rebuiltLots.Add(new Lot
                {
                    Quantity = pendingQuantity,
                    EntryPrice = referencePrice,
                    HighestPrice = highestPrice,
                    TrailingActive = false,
                    PendingSell = true,
                    PendingSellOrderId = ticket.OrderId
                });

                remainingQuantity -= pendingQuantity;
            }

            if (remainingQuantity > 0)
            {
                rebuiltLots.Add(new Lot
                {
                    Quantity = remainingQuantity,
                    EntryPrice = referencePrice,
                    HighestPrice = highestPrice,
                    TrailingActive = false,
                    PendingSell = false,
                    PendingSellOrderId = 0
                });
            }

            if (TryGetInvariantViolationForLots(rebuiltLots, currentQuantity, openSellTickets, out var rebuildViolation))
            {
                Error($"[REBUILD] {Time} Rebuilt lot state failed validation. Symbol={symbolData.Symbol} Violation={rebuildViolation} Reason={reason}");
                return false;
            }

            symbolData.Lots = rebuiltLots;
            if (LiveMode)
            {
                SaveState($"Rebuild {symbolData.Symbol.Value}");
            }

            Debug($"[REBUILD] {Time} Rebuilt tracked lots from holdings/open orders. Symbol={symbolData.Symbol} Holdings={currentQuantity} OpenSellOrders={openSellTickets.Count} Lots={previousLotCount}->{rebuiltLots.Count} Pending={previousPendingCount}->{rebuiltLots.Count(lot => lot.PendingSell)} TrackedQty={previousTrackedQuantity}->{rebuiltLots.Sum(lot => lot.Quantity)} Reason={reason}");
            return true;
        }

        private bool BlockSymbol(SymbolData symbolData, string reason)
        {
            if (symbolData.TradingBlocked && symbolData.BlockedReason == reason)
            {
                return false;
            }

            symbolData.TradingBlocked = true;
            symbolData.BlockedReason = reason;
            Error($"[BLOCKED] {Time} Trading blocked for symbol until broker state can be reconciled. Symbol={symbolData.Symbol} Reason={reason}");
            return true;
        }

        private static bool TryGetInvariantViolationForLots(List<Lot> lots, int currentQuantity, List<OrderTicket> openSellTickets, out string violation)
        {
            var symbolData = new SymbolData
            {
                Lots = lots
            };

            return TryGetInvariantViolation(symbolData, currentQuantity, openSellTickets, out violation);
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
                if (!EnsureSymbolStateReady(symbolData, "Pre-trade validation"))
                {
                    continue;
                }

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

            if (symbolData.TradingBlocked)
            {
                EnsureSymbolStateReady(symbolData, $"Order event while symbol blocked. OrderId={orderEvent.OrderId}");
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

            if (!EnsureSymbolStateReady(symbolData, $"Post-order-event validation. OrderId={orderEvent.OrderId}"))
            {
                return;
            }

            if (stateChanged && LiveMode)
            {
                SaveState($"Order event {orderEvent.OrderId}");
            }
        }

        private bool HandleSellFill(SymbolData symbolData, OrderEvent orderEvent)
        {
            var quantityToRemove = (int)Math.Abs(orderEvent.FillQuantity);
            var lot = symbolData.Lots.FirstOrDefault(l => l.PendingSellOrderId == orderEvent.OrderId);
            if (lot == null)
            {
                Error($"[REBUILD] {Time} Sell fill without matching tracked lot. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId}");
                return TryRebuildTrackedLotsFromBrokerState(symbolData, $"Sell fill without matching tracked lot. OrderId={orderEvent.OrderId}");
            }

            if (quantityToRemove > lot.Quantity)
            {
                Error($"[REBUILD] {Time} Sell fill exceeds tracked lot quantity. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId} FillQuantity={quantityToRemove} LotQuantity={lot.Quantity}");
                return TryRebuildTrackedLotsFromBrokerState(symbolData, $"Sell fill exceeds tracked lot quantity. OrderId={orderEvent.OrderId}");
            }

            lot.Quantity -= quantityToRemove;
            if (!IsResolvedSellStatus(orderEvent.Status))
            {
                symbolData.Lots.RemoveAll(l => l.Quantity <= 0);
            }

            return true;
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
                Error($"[REBUILD] {Time} Sell order resolved without matching tracked lot. Symbol={orderEvent.Symbol} OrderId={orderEvent.OrderId} Status={orderEvent.Status}");
                return TryRebuildTrackedLotsFromBrokerState(symbolData, $"Sell order resolved without matching tracked lot. OrderId={orderEvent.OrderId} Status={orderEvent.Status}");
            }

            lot.PendingSell = false;
            lot.PendingSellOrderId = 0;
            if (lot.Quantity <= 0)
            {
                symbolData.Lots.Remove(lot);
            }

            return true;
        }

        private void SaveState(string reason = null)
        {
            try
            {
                var state = _symbolDataMap
                    .Where(kvp => kvp.Value.Lots.Any())
                    .ToDictionary(kvp => kvp.Key.Value, kvp => kvp.Value.Lots);

                var fingerprint = BuildStateFingerprint(state);
                if (fingerprint == _lastSavedStateFingerprint)
                {
                    Debug($"[CloudSave] {Time}: state unchanged, skip save. Reason={reason ?? "unspecified"}");
                    return;
                }

                ObjectStore.SaveJson(StateKey, state);
                _lastSavedStateFingerprint = fingerprint;
                Debug($"[CloudSave] {Time}: state saved. Symbols tracked: {string.Join(", ", state.Keys.OrderBy(x => x))} Reason={reason ?? "unspecified"}");
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
                Debug($"[CloudLoad] {Time}: no persisted state found for key {StateKey}.");
                _lastSavedStateFingerprint = BuildStateFingerprint(new Dictionary<string, List<Lot>>());
                return;
            }

            try
            {
                var loaded = ObjectStore.ReadJson<Dictionary<string, List<Lot>>>(StateKey);
                if (loaded == null)
                {
                    Error($"[CloudLoad] {Time}: persisted state was null for key {StateKey}.");
                    _lastSavedStateFingerprint = BuildStateFingerprint(new Dictionary<string, List<Lot>>());
                    return;
                }

                foreach (var kvp in loaded)
                {
                    var symbolData = _symbolDataMap.Values.FirstOrDefault(s => s.Symbol.Value == kvp.Key);
                    if (symbolData != null)
                    {
                        symbolData.Lots = kvp.Value;
                    }
                }

                _lastSavedStateFingerprint = BuildStateFingerprint(loaded);

                var totalLots = loaded.Sum(kvp => kvp.Value?.Count ?? 0);
                var totalPending = loaded.Sum(kvp => kvp.Value?.Count(lot => lot.PendingSell) ?? 0);
                Debug($"[CloudLoad] {Time}: restored lot details for {loaded.Count} symbols. Lots={totalLots} PendingSells={totalPending}");
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
                SaveState("Algorithm end");
            }
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(StateKey))
            {
                throw new InvalidOperationException("StateKey must not be empty.");
            }

            if (StateKey.Length > 128)
            {
                throw new InvalidOperationException($"StateKey is unexpectedly long ({StateKey.Length}).");
            }

            if (_config.Count == 0)
            {
                throw new InvalidOperationException("At least one symbol must be configured.");
            }

            if (_buyStep <= 0 || _buyStep > 1)
            {
                throw new InvalidOperationException($"buy-step must be in (0, 1]. Current value: {_buyStep}");
            }

            if (_rebalanceThreshold < 0 || _rebalanceThreshold > 1)
            {
                throw new InvalidOperationException($"rebalance-threshold must be in [0, 1]. Current value: {_rebalanceThreshold}");
            }

            foreach (var kvp in _config)
            {
                var ticker = kvp.Key;
                var settings = kvp.Value;

                if (string.IsNullOrWhiteSpace(ticker))
                {
                    throw new InvalidOperationException("Configured ticker must not be empty.");
                }

                if (settings == null)
                {
                    throw new InvalidOperationException($"Settings are missing for ticker {ticker}.");
                }

                if (settings.SmaLength <= 0)
                {
                    throw new InvalidOperationException($"SmaLength must be positive for {ticker}.");
                }

                if (settings.BuyThreshold < 0 || settings.BuyThreshold >= 1)
                {
                    throw new InvalidOperationException($"BuyThreshold must be in [0, 1) for {ticker}. Current value: {settings.BuyThreshold}");
                }

                if (settings.TakeProfitUp <= 0 || settings.TakeProfitUp >= 1)
                {
                    throw new InvalidOperationException($"TakeProfitUp must be in (0, 1) for {ticker}. Current value: {settings.TakeProfitUp}");
                }

                if (settings.StopLoss <= 0 || settings.StopLoss >= 1)
                {
                    throw new InvalidOperationException($"StopLoss must be in (0, 1) for {ticker}. Current value: {settings.StopLoss}");
                }

                if (settings.TrailingDrop <= 0 || settings.TrailingDrop >= 1)
                {
                    throw new InvalidOperationException($"TrailingDrop must be in (0, 1) for {ticker}. Current value: {settings.TrailingDrop}");
                }

                if (settings.MaxWeight <= 0 || settings.MaxWeight > 1)
                {
                    throw new InvalidOperationException($"MaxWeight must be in (0, 1] for {ticker}. Current value: {settings.MaxWeight}");
                }
            }
        }

        private static string BuildStateFingerprint(Dictionary<string, List<Lot>> state)
        {
            var builder = new StringBuilder();

            foreach (var symbol in state.Keys.OrderBy(x => x))
            {
                builder.Append(symbol).Append('|');

                var lots = state[symbol] ?? new List<Lot>();
                foreach (var lot in lots.OrderBy(lot => lot.PendingSellOrderId).ThenBy(lot => lot.EntryPrice).ThenBy(lot => lot.Quantity))
                {
                    builder.Append(lot.Quantity).Append(':')
                        .Append(lot.EntryPrice).Append(':')
                        .Append(lot.HighestPrice).Append(':')
                        .Append(lot.TrailingActive ? '1' : '0').Append(':')
                        .Append(lot.PendingSell ? '1' : '0').Append(':')
                        .Append(lot.PendingSellOrderId)
                        .Append(';');
                }

                builder.Append('#');
            }

            return builder.ToString();
        }
    }
}
