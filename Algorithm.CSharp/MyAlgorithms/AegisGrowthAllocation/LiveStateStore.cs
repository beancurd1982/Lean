#region imports
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuantConnect;
using QuantConnect.Algorithm;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    public sealed class LiveStateStore
    {
        private readonly QCAlgorithm _algorithm;
        private string _lastSavedFingerprint;

        public LiveStateStore(QCAlgorithm algorithm)
        {
            _algorithm = algorithm;
            _lastSavedFingerprint = BuildFingerprint(new AegisLiveState { SchemaVersion = StrategyConfig.LiveStateSchemaVersion });
        }

        public AegisLiveState Load()
        {
            if (!_algorithm.ObjectStore.ContainsKey(StrategyConfig.LiveStateKey))
            {
                _algorithm.Debug($"[AEGIS-LIVE] {_algorithm.Time}: no persisted state found for key {StrategyConfig.LiveStateKey}.");
                _lastSavedFingerprint = BuildFingerprint(new AegisLiveState { SchemaVersion = StrategyConfig.LiveStateSchemaVersion });
                return null;
            }

            try
            {
                var state = _algorithm.ObjectStore.ReadJson<AegisLiveState>(StrategyConfig.LiveStateKey);
                if (state == null)
                {
                    _algorithm.Error($"[AEGIS-LIVE] {_algorithm.Time}: persisted state was null for key {StrategyConfig.LiveStateKey}.");
                    _lastSavedFingerprint = BuildFingerprint(new AegisLiveState { SchemaVersion = StrategyConfig.LiveStateSchemaVersion });
                    return null;
                }

                if (state.SchemaVersion != StrategyConfig.LiveStateSchemaVersion)
                {
                    _algorithm.Error($"[AEGIS-LIVE] {_algorithm.Time}: persisted state schema mismatch. Found={state.SchemaVersion} Expected={StrategyConfig.LiveStateSchemaVersion}");
                    _lastSavedFingerprint = BuildFingerprint(new AegisLiveState { SchemaVersion = StrategyConfig.LiveStateSchemaVersion });
                    return null;
                }

                NormalizeState(state);

                _lastSavedFingerprint = BuildFingerprint(state);

                _algorithm.Debug(
                    $"[AEGIS-LIVE] {_algorithm.Time}: state restored. Holdings={state.BrokerHoldingsByTicker.Count} OpenOrders={state.OpenOrders.Count} LastReview={state.LastCompletedWeeklyReviewUtc?.ToString("u") ?? "none"} DefensiveHighWater={state.DefensiveOverrideEquityHighWaterMark:0.##} SevereActive={state.SevereCrashModeActive} SevereState={state.SevereCrashModeState} SevereWeeks={state.SevereCrashRecoveryWeeks} SevereExit={state.SevereCrashExitReason}");
                return state;
            }
            catch (Exception ex)
            {
                _algorithm.Error($"[AEGIS-LIVE] {_algorithm.Time}: load error: {ex.Message}");
                _lastSavedFingerprint = BuildFingerprint(new AegisLiveState { SchemaVersion = StrategyConfig.LiveStateSchemaVersion });
                return null;
            }
        }

        public void Save(AegisLiveState state, string reason = null)
        {
            if (state == null)
            {
                return;
            }

            state.SchemaVersion = StrategyConfig.LiveStateSchemaVersion;
            state.SavedAtUtc = _algorithm.UtcTime;
            NormalizeState(state);

            try
            {
                var fingerprint = BuildFingerprint(state);
                if (fingerprint == _lastSavedFingerprint)
                {
                    _algorithm.Debug($"[AEGIS-LIVE] {_algorithm.Time}: state unchanged, skip save. Reason={reason ?? "unspecified"}");
                    return;
                }

                _algorithm.ObjectStore.SaveJson(StrategyConfig.LiveStateKey, state);
                _lastSavedFingerprint = fingerprint;
                _algorithm.Debug(
                    $"[AEGIS-LIVE] {_algorithm.Time}: state saved. Holdings={state.BrokerHoldingsByTicker.Count} OpenOrders={state.OpenOrders.Count} DefensiveHighWater={state.DefensiveOverrideEquityHighWaterMark:0.##} SevereActive={state.SevereCrashModeActive} SevereState={state.SevereCrashModeState} SevereWeeks={state.SevereCrashRecoveryWeeks} SevereExit={state.SevereCrashExitReason} Reason={reason ?? "unspecified"}");
            }
            catch (Exception ex)
            {
                _algorithm.Error($"[AEGIS-LIVE] {_algorithm.Time}: save error: {ex.Message}");
            }
        }

        private static void NormalizeState(AegisLiveState state)
        {
            state.LastPlannedTargetWeights ??= new Dictionary<string, decimal>();
            state.BrokerHoldingsByTicker ??= new Dictionary<string, decimal>();
            state.OpenOrders ??= new List<AegisOpenOrderState>();
            state.DefensiveOverrideEquityHighWaterMark = Math.Max(0m, state.DefensiveOverrideEquityHighWaterMark);
            state.SevereCrashRecoveryWeeks = Math.Max(0, state.SevereCrashRecoveryWeeks);
            state.SevereCrashModeState = string.IsNullOrWhiteSpace(state.SevereCrashModeState)
                ? "none"
                : state.SevereCrashModeState;
            state.SevereCrashExitReason = string.IsNullOrWhiteSpace(state.SevereCrashExitReason)
                ? "none"
                : state.SevereCrashExitReason;
        }

        private static string BuildFingerprint(AegisLiveState state)
        {
            var builder = new StringBuilder();
            builder.Append("v=").Append(state.SchemaVersion).Append('|');
            builder.Append("regime=").Append((int)state.ActiveRegime).Append('|');
            builder.Append("upgrade=").Append(state.UpgradeConfirmationCount).Append('|');
            builder.Append("reserve=").Append(state.UndeployedReserve.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture)).Append('|');
            builder.Append("review=").Append(state.LastCompletedWeeklyReviewUtc?.ToString("o") ?? string.Empty).Append('|');
            builder.Append("defHwm=").Append(state.DefensiveOverrideEquityHighWaterMark.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture)).Append('|');
            builder.Append("sevActive=").Append(state.SevereCrashModeActive).Append('|');
            builder.Append("sevWeeks=").Append(state.SevereCrashRecoveryWeeks).Append('|');
            builder.Append("sevState=").Append(state.SevereCrashModeState ?? string.Empty).Append('|');
            builder.Append("sevExit=").Append(state.SevereCrashExitReason ?? string.Empty).Append('|');

            foreach (var holding in (state.BrokerHoldingsByTicker ?? new Dictionary<string, decimal>()).OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
            {
                builder.Append("hold=").Append(holding.Key).Append(':')
                    .Append(holding.Value.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture)).Append('|');
            }

            foreach (var target in (state.LastPlannedTargetWeights ?? new Dictionary<string, decimal>()).OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
            {
                builder.Append("target=").Append(target.Key).Append(':')
                    .Append(target.Value.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture)).Append('|');
            }

            foreach (var order in (state.OpenOrders ?? new List<AegisOpenOrderState>()).OrderBy(order => order.OrderId))
            {
                builder.Append("order=").Append(order.OrderId).Append(':')
                    .Append(order.Ticker).Append(':')
                    .Append(order.Direction).Append(':')
                    .Append(order.Quantity.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture)).Append(':')
                    .Append(order.Status).Append('|');
            }

            return builder.ToString();
        }
    }

    public sealed class AegisLiveState
    {
        public int SchemaVersion { get; set; }
        public DateTime SavedAtUtc { get; set; }
        public RiskRegime ActiveRegime { get; set; }
        public int UpgradeConfirmationCount { get; set; }
        public decimal UndeployedReserve { get; set; }
        public DateTime? LastCompletedWeeklyReviewUtc { get; set; }
        public decimal DefensiveOverrideEquityHighWaterMark { get; set; }
        public bool SevereCrashModeActive { get; set; }
        public int SevereCrashRecoveryWeeks { get; set; }
        public string SevereCrashModeState { get; set; } = "none";
        public string SevereCrashExitReason { get; set; } = "none";
        public Dictionary<string, decimal> LastPlannedTargetWeights { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> BrokerHoldingsByTicker { get; set; } = new Dictionary<string, decimal>();
        public List<AegisOpenOrderState> OpenOrders { get; set; } = new List<AegisOpenOrderState>();
    }

    public sealed class AegisOpenOrderState
    {
        public int OrderId { get; set; }
        public string Ticker { get; set; }
        public string Direction { get; set; }
        public decimal Quantity { get; set; }
        public string Status { get; set; }
    }
}
