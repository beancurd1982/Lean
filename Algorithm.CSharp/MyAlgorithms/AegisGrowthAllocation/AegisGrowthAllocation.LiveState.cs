#region imports
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuantConnect;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    public partial class AegisGrowthAllocation
    {
        private void RestorePersistedRuntimeState(AegisLiveState state)
        {
            if (state == null)
            {
                return;
            }

            _regimeModel.Restore(state.ActiveRegime, state.UpgradeConfirmationCount);
            _undeployedCapitalReserve = Math.Max(0m, state.UndeployedReserve);
            _lastCompletedWeeklyReviewUtc = state.LastCompletedWeeklyReviewUtc;
            _lastPlannedTargetWeights = state.LastPlannedTargetWeights
                .Where(kvp => TryGetTrackedSymbol(kvp.Key, out _))
                .ToDictionary(
                    kvp =>
                    {
                        TryGetTrackedSymbol(kvp.Key, out var symbol);
                        return symbol;
                    },
                    kvp => kvp.Value);
            _defensiveOverrideEquityHighWaterMark = Math.Max(0m, state.DefensiveOverrideEquityHighWaterMark);
            _severeCrashModeActive = state.SevereCrashModeActive;
            _severeCrashRecoveryWeeks = Math.Max(0, state.SevereCrashRecoveryWeeks);
            _severeCrashModeState = string.IsNullOrWhiteSpace(state.SevereCrashModeState)
                ? "none"
                : state.SevereCrashModeState;
            _severeCrashExitReason = string.IsNullOrWhiteSpace(state.SevereCrashExitReason)
                ? "none"
                : state.SevereCrashExitReason;
        }

        private bool ReconcileLiveStartup()
        {
            try
            {
                var brokerHoldings = CaptureBrokerHoldingsByTicker();
                var brokerOpenOrders = CaptureBrokerOpenOrders();
                var persistedHoldings = _loadedLiveState?.BrokerHoldingsByTicker ?? new Dictionary<string, decimal>();
                var persistedOpenOrders = _loadedLiveState?.OpenOrders ?? new List<AegisOpenOrderState>();

                LogLiveStateEvidence("Startup", _loadedLiveState, brokerHoldings, brokerOpenOrders.Count);

                var holdingsMatch = DictionariesMatch(brokerHoldings, persistedHoldings);
                var openOrdersMatch = OpenOrdersMatch(brokerOpenOrders.Values, persistedOpenOrders);

                if (!holdingsMatch || !openOrdersMatch)
                {
                    Debug(
                        $"[AEGIS-LIVE] {Time}: broker/store mismatch detected. BrokerHoldings={brokerHoldings.Count} StoreHoldings={persistedHoldings.Count} BrokerOpenOrders={brokerOpenOrders.Count} StoreOpenOrders={persistedOpenOrders.Count}. Broker state wins.");
                }
                else
                {
                    Debug($"[AEGIS-LIVE] {Time}: broker/store state matched on startup.");
                }

                _trackedOpenOrders = brokerOpenOrders;
                _startupReconciliationComplete = true;
                Debug(
                    $"[AEGIS-LIVE] {Time}: startup reconciliation complete. Holdings={brokerHoldings.Count} OpenOrders={_trackedOpenOrders.Count} RestoredRegime={_regimeModel.ActiveRegime} LastReview={_lastCompletedWeeklyReviewUtc?.ToString("u") ?? "none"}");

                if (ShouldDeferStartupStateSave(_loadedLiveState, brokerHoldings, brokerOpenOrders))
                {
                    Debug(
                        $"[AEGIS-LIVE] {Time}: startup state save deferred because no persisted or broker holdings/open orders are visible during Initialize.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _startupReconciliationComplete = false;
                Error($"[AEGIS-LIVE] {Time}: startup reconciliation failed: {ex.Message}");
                return false;
            }
        }

        private void TryCompleteDeferredStartupStateSave(string phase)
        {
            var brokerHoldings = CaptureBrokerHoldingsByTicker();
            var brokerOpenOrders = CaptureBrokerOpenOrders();
            LogLiveStateEvidence(phase, _loadedLiveState, brokerHoldings, brokerOpenOrders.Count);

            if (!_startupStateSaveDeferred)
            {
                return;
            }

            if (ShouldDeferStartupStateSave(_loadedLiveState, brokerHoldings, brokerOpenOrders))
            {
                Debug(
                    $"[AEGIS-LIVE] {Time}: deferred startup state save remains skipped at {phase} because no persisted or broker holdings/open orders are visible.");
                return;
            }

            _trackedOpenOrders = brokerOpenOrders;
            _startupStateSaveDeferred = false;
            SaveLiveState($"Deferred startup reconciliation after {phase}");
        }

        private void SaveLiveState(string reason)
        {
            if (!LiveMode)
            {
                return;
            }

            RefreshTrackedOpenOrdersFromBroker();
            _liveStateStore.Save(BuildPersistedState(), reason);
        }

        private AegisLiveState BuildPersistedState()
        {
            return new AegisLiveState
            {
                SchemaVersion = StrategyConfig.LiveStateSchemaVersion,
                AlgorithmVersion = StrategyConfig.AlgorithmVersion,
                SourceRevision = StrategyConfig.SourceRevision,
                ActiveRegime = _regimeModel.ActiveRegime,
                UpgradeConfirmationCount = _regimeModel.UpgradeConfirmationCount,
                UndeployedReserve = _undeployedCapitalReserve,
                LastCompletedWeeklyReviewUtc = _lastCompletedWeeklyReviewUtc,
                DefensiveOverrideEquityHighWaterMark = _defensiveOverrideEquityHighWaterMark,
                SevereCrashModeActive = _severeCrashModeActive,
                SevereCrashRecoveryWeeks = _severeCrashRecoveryWeeks,
                SevereCrashModeState = _severeCrashModeState,
                SevereCrashExitReason = _severeCrashExitReason,
                LastPlannedTargetWeights = _lastPlannedTargetWeights
                    .ToDictionary(kvp => kvp.Key.Value, kvp => kvp.Value, StringComparer.Ordinal),
                BrokerHoldingsByTicker = CaptureBrokerHoldingsByTicker(),
                OpenOrders = _trackedOpenOrders.Values
                    .OrderBy(order => order.OrderId)
                    .Select(order => new AegisOpenOrderState
                    {
                        OrderId = order.OrderId,
                        Ticker = order.Ticker,
                        Direction = order.Direction,
                        Quantity = order.Quantity,
                        Status = order.Status
                    })
                    .ToList()
            };
        }

        private Dictionary<string, decimal> CaptureBrokerHoldingsByTicker()
        {
            var holdings = new Dictionary<string, decimal>(StringComparer.Ordinal);

            foreach (var assetState in _assetStates.Values)
            {
                var quantity = Portfolio[assetState.Symbol].Quantity;
                if (Math.Abs(quantity) <= StrategyConfig.LiveStateQuantityTolerance)
                {
                    continue;
                }

                holdings[assetState.Ticker] = quantity;
            }

            return holdings;
        }

        private Dictionary<int, AegisOpenOrderState> CaptureBrokerOpenOrders()
        {
            return Transactions.GetOpenOrders()
                .Where(order => _assetStates.ContainsKey(order.Symbol))
                .ToDictionary(
                    order => order.Id,
                    order => new AegisOpenOrderState
                    {
                        OrderId = order.Id,
                        Ticker = order.Symbol.Value,
                        Direction = order.Direction.ToString(),
                        Quantity = order.Quantity,
                        Status = order.Status.ToString()
                    });
        }

        private void LogLiveStateEvidence(
            string phase,
            AegisLiveState persistedState,
            IReadOnlyDictionary<string, decimal> brokerHoldings,
            int brokerOpenOrderCount)
        {
            var persistedHoldings = persistedState?.BrokerHoldingsByTicker ?? new Dictionary<string, decimal>();
            var persistedOpenOrderCount = persistedState?.OpenOrders?.Count ?? 0;
            Debug(
                $"[AEGIS-LIVE-DIAG] {Time}: Phase={phase} PersistedHoldings={persistedHoldings.Count} PersistedTickers={FormatLiveHoldingsSummary(persistedHoldings)} BrokerHoldings={brokerHoldings.Count} BrokerTickers={FormatLiveHoldingsSummary(brokerHoldings)} PersistedOpenOrders={persistedOpenOrderCount} BrokerOpenOrders={brokerOpenOrderCount}");
        }

        private static bool ShouldDeferStartupStateSave(
            AegisLiveState persistedState,
            IReadOnlyDictionary<string, decimal> brokerHoldings,
            IReadOnlyDictionary<int, AegisOpenOrderState> brokerOpenOrders)
        {
            var persistedHoldingCount = persistedState?.BrokerHoldingsByTicker?.Count ?? 0;
            var persistedOpenOrderCount = persistedState?.OpenOrders?.Count ?? 0;
            if (persistedHoldingCount > 0 && brokerHoldings.Count == 0)
            {
                return true;
            }

            return persistedHoldingCount == 0 &&
                persistedOpenOrderCount == 0 &&
                brokerHoldings.Count == 0 &&
                brokerOpenOrders.Count == 0;
        }

        private static string FormatLiveHoldingsSummary(IReadOnlyDictionary<string, decimal> holdings)
        {
            if (holdings.Count == 0)
            {
                return "none";
            }

            return string.Join(
                ";",
                holdings
                    .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                    .Select(kvp => $"{kvp.Key}={kvp.Value.ToString("0.########", CultureInfo.InvariantCulture)}"));
        }

        private void RefreshTrackedOpenOrdersFromBroker()
        {
            _trackedOpenOrders = CaptureBrokerOpenOrders();
        }

        private bool TryGetTrackedSymbol(string ticker, out Symbol symbol)
        {
            var assetState = _assetStates.Values.FirstOrDefault(state => state.Ticker.Equals(ticker, StringComparison.Ordinal));
            if (assetState != null)
            {
                symbol = assetState.Symbol;
                return true;
            }

            symbol = null;
            return false;
        }

        private static bool DictionariesMatch(
            IReadOnlyDictionary<string, decimal> left,
            IReadOnlyDictionary<string, decimal> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            foreach (var kvp in left)
            {
                if (!right.TryGetValue(kvp.Key, out var value))
                {
                    return false;
                }

                if (Math.Abs(kvp.Value - value) > StrategyConfig.LiveStateQuantityTolerance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool OpenOrdersMatch(
            IEnumerable<AegisOpenOrderState> brokerOrders,
            IEnumerable<AegisOpenOrderState> persistedOrders)
        {
            var brokerList = brokerOrders
                .OrderBy(order => order.OrderId)
                .ToList();
            var persistedList = persistedOrders
                .OrderBy(order => order.OrderId)
                .ToList();

            if (brokerList.Count != persistedList.Count)
            {
                return false;
            }

            for (var index = 0; index < brokerList.Count; index++)
            {
                var broker = brokerList[index];
                var persisted = persistedList[index];
                if (broker.OrderId != persisted.OrderId ||
                    !string.Equals(broker.Ticker, persisted.Ticker, StringComparison.Ordinal) ||
                    !string.Equals(broker.Direction, persisted.Direction, StringComparison.Ordinal) ||
                    !string.Equals(broker.Status, persisted.Status, StringComparison.Ordinal) ||
                    Math.Abs(broker.Quantity - persisted.Quantity) > StrategyConfig.LiveStateQuantityTolerance)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
