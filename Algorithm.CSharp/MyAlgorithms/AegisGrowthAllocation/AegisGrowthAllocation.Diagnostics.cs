#region imports
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    public partial class AegisGrowthAllocation : QCAlgorithm
    {        private void LogStressDiagnostic(string phase)
        {
            if (!LiveMode)
            {
                return;
            }

            var stressCount = _stressWindow.Count;
            var latestStressClose = stressCount > 0
                ? (decimal?)_stressWindow[0]
                : null;
            var stressReady = stressCount >= StrategyConfig.VixAverageWindow;
            var stressAverage5 = stressReady
                ? (decimal?)_stressWindow.Average()
                : null;

            Debug(FormatStressDiagnostic(
                phase,
                _stressSymbol?.Value ?? StrategyConfig.StressTicker,
                stressCount,
                latestStressClose,
                stressAverage5,
                stressReady));
        }

        private static string FormatStressDiagnostic(
            string phase,
            string stressSymbol,
            int stressCount,
            decimal? latestStressClose,
            decimal? stressAverage5,
            bool isReady)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "[AEGIS-STRESS-DIAG] Phase={0} StressSymbol={1} StressCount={2} LatestStressClose={3} StressAverage5={4} StressReady={5}",
                phase,
                stressSymbol,
                stressCount,
                latestStressClose.HasValue ? latestStressClose.Value.ToString("0.####", CultureInfo.InvariantCulture) : "none",
                stressAverage5.HasValue ? stressAverage5.Value.ToString("0.####", CultureInfo.InvariantCulture) : "none",
                isReady);
        }

        private string FormatWeeklySummary(PortfolioPlan plan, RegimeSnapshot regimeSnapshot)
        {
            var currentCashWeight = Math.Max(0m, 1m - plan.CurrentGrowthWeight - plan.CurrentDefensiveWeight);
            var targetGrowthWeight = plan.TargetWeights
                .Where(pair => _assetStates.TryGetValue(pair.Key, out var assetState) && assetState.IsGrowth)
                .Sum(pair => pair.Value);
            var targetDefensiveWeight = plan.TargetWeights
                .Where(pair => _assetStates.TryGetValue(pair.Key, out var assetState) && assetState.IsDefensive)
                .Sum(pair => pair.Value);
            var targetCashWeight = Math.Max(0m, 1m - targetGrowthWeight - targetDefensiveWeight);

            return string.Format(
                CultureInfo.InvariantCulture,
                "[AEGIS] {0:yyyy-MM-dd} Prev={1} Act={2} Raw={3} Trend={4} Breadth={5} Stress={6} Severe={7} Curr=G{8:0.00}/D{9:0.00}/C{10:0.00} Target=G{11:0.00}/D{12:0.00}/C{13:0.00} Growth{14} Def{15} Forced={16} Trim={17} Reserve={18:0.##}",
                Time,
                plan.PreviousRegime,
                plan.ActiveRegime,
                regimeSnapshot.RawRegime,
                regimeSnapshot.TrendState,
                regimeSnapshot.BreadthState,
                regimeSnapshot.StressState,
                regimeSnapshot.SevereStress,
                plan.CurrentGrowthWeight,
                plan.CurrentDefensiveWeight,
                currentCashWeight,
                targetGrowthWeight,
                targetDefensiveWeight,
                targetCashWeight,
                FormatSymbolPreview(plan.SelectedGrowthSymbols),
                FormatSymbolPreview(plan.SelectedDefensiveSymbols),
                plan.ForcedExitSymbols.Count,
                plan.TrimOnly,
                plan.ReleasedReserve);
        }

        private string FormatCrisisDiagnostics(
            PortfolioPlan plan,
            RegimeSnapshot regimeSnapshot,
            GrowthSelection growthSelection,
            DefensiveSelection defensiveSelection,
            IReadOnlyDictionary<Symbol, decimal> currentWeights,
            decimal breadth,
            decimal vixAverage5,
            decimal reserveBeforeReview,
            bool preWeakGuardActive,
            bool severeCrashOverrideActive,
            string sleeveOverride,
            string overrideReason,
            bool preWeakRecoveryActive,
            int preWeakRecoverySegmentId,
            decimal preWeakRecoveryLocalTroughDrawdown,
            decimal preWeakRecoveryAmount,
            int preWeakRecoveryConfirmationWeeks,
            string preWeakRecoveryResetReason,
            decimal drawdownFromHigh,
            SleeveTargets baseSleeveTargets,
            SleeveTargets finalSleeveTargets,
            string severeCrashModeState,
            int severeCrashRecoveryWeeks,
            string severeCrashExitReason)
        {
            var currentCashWeight = Math.Max(0m, 1m - plan.CurrentGrowthWeight - plan.CurrentDefensiveWeight);
            var targetGrowthWeight = plan.TargetWeights
                .Where(pair => _assetStates.TryGetValue(pair.Key, out var assetState) && assetState.IsGrowth)
                .Sum(pair => pair.Value);
            var targetDefensiveWeight = plan.TargetWeights
                .Where(pair => _assetStates.TryGetValue(pair.Key, out var assetState) && assetState.IsDefensive)
                .Sum(pair => pair.Value);
            var targetCashWeight = Math.Max(0m, 1m - targetGrowthWeight - targetDefensiveWeight);

            return string.Format(
                CultureInfo.InvariantCulture,
                "[AEGIS-DIAG-WEEK] {0:yyyy-MM-dd} Eq={1:0.00} Act={2} Raw={3} Trend={4} Breadth={5}/{6:0.0000} Stress={7}/{8:0.00} Severe={9} DD={10:0.0000} Override={11} Reason={12} PreWeakRecovery={13} RecoverySegment={14} RecoveryTroughDD={15:0.0000} RecoveryAmount={16:0.0000} RecoveryConfirm={17} RecoveryReset={18} Curr=G{19:0.0000}/D{20:0.0000}/C{21:0.0000} Target=G{22:0.0000}/D{23:0.0000}/C{24:0.0000} Sleeve={25} Rebalance={26} SelectionChange={27} Trim={28} Forced={29} OptRepl={30} NewEntries={31} Reserve={32:0.####}->{33:0.####} Released={34:0.####} Growth={35} Defensive={36} TopGrowth={37} TopDef={38}",
                Time,
                Portfolio.TotalPortfolioValue,
                plan.ActiveRegime,
                regimeSnapshot.RawRegime,
                regimeSnapshot.TrendState,
                regimeSnapshot.BreadthState,
                breadth,
                regimeSnapshot.StressState,
                vixAverage5,
                regimeSnapshot.SevereStress,
                drawdownFromHigh,
                sleeveOverride,
                overrideReason,
                preWeakRecoveryActive,
                preWeakRecoverySegmentId,
                preWeakRecoveryLocalTroughDrawdown,
                preWeakRecoveryAmount,
                preWeakRecoveryConfirmationWeeks,
                preWeakRecoveryResetReason,
                plan.CurrentGrowthWeight,
                plan.CurrentDefensiveWeight,
                currentCashWeight,
                targetGrowthWeight,
                targetDefensiveWeight,
                targetCashWeight,
                FormatSleeveTargets(finalSleeveTargets),
                plan.HasRebalanceTrigger,
                plan.HasSelectionChange,
                plan.TrimOnly,
                FormatSymbolList(plan.ForcedExitSymbols),
                plan.OptimizationReplacementsUsed,
                plan.NewEntriesUsed,
                reserveBeforeReview,
                _undeployedCapitalReserve,
                plan.ReleasedReserve,
                FormatSymbolList(plan.SelectedGrowthSymbols),
                FormatSymbolList(plan.SelectedDefensiveSymbols),
                FormatGrowthCandidateScores(growthSelection.RankedCandidates, maxCount: 3),
                FormatDefensiveCandidateScores(defensiveSelection.RankedCandidates, maxCount: 3));
        }

        private string FormatOverrideDiagnostics(
            bool preWeakGuardActive,
            bool severeCrashOverrideActive,
            string sleeveOverride,
            string overrideReason,
            decimal drawdownFromHigh,
            SleeveTargets baseSleeveTargets,
            SleeveTargets finalSleeveTargets,
            string severeCrashModeState,
            int severeCrashRecoveryWeeks,
            string severeCrashExitReason)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "PreWeakGuardActive={0} SevereCrashOverrideActive={1} SleeveOverride={2} OverrideReason={3} DrawdownFromHigh={4:0.0000} BaseTarget={5} FinalTarget={6} SevereCrashModeState={7} SevereCrashRecoveryWeeks={8} SevereCrashExitReason={9}",
                preWeakGuardActive,
                severeCrashOverrideActive,
                sleeveOverride,
                overrideReason,
                drawdownFromHigh,
                FormatSleeveTargets(baseSleeveTargets),
                FormatSleeveTargets(finalSleeveTargets),
                severeCrashModeState,
                severeCrashRecoveryWeeks,
                severeCrashExitReason);
        }

        private void RecordCrisisDiagnosticObservation(
            DateTime date,
            decimal equity,
            RiskRegime activeRegime,
            bool preWeakGuardActive,
            bool preWeakRecoveryActive,
            bool severeCrashOverrideActive,
            decimal drawdownFromHigh,
            SleeveTargets finalSleeveTargets)
        {
            _diagnosticAttributionTracker.Record(
                date,
                equity,
                activeRegime,
                preWeakGuardActive,
                preWeakRecoveryActive,
                severeCrashOverrideActive,
                drawdownFromHigh,
                finalSleeveTargets);
        }

        private string FormatCompactCrisisDiagnosticSummary()
        {
            return _diagnosticAttributionTracker.FormatSummary();
        }

        private static string FormatSleeveTargets(SleeveTargets targets)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "G{0:0.0000}/D{1:0.0000}/C{2:0.0000}",
                targets.GrowthTarget,
                targets.DefensiveTarget,
                targets.CashTarget);
        }

        private static string FormatSymbolPreview(IReadOnlyList<Symbol> symbols)
        {
            if (symbols.Count == 0)
            {
                return "[0]";
            }

            const int previewCount = 3;
            var preview = string.Join(",", symbols.Take(previewCount).Select(symbol => symbol.Value));
            var extraCount = symbols.Count - previewCount;

            return extraCount > 0
                ? $"[{symbols.Count}]={preview}+{extraCount}"
                : $"[{symbols.Count}]={preview}";
        }

        private static string FormatSymbolList(IEnumerable<Symbol> symbols)
        {
            var values = symbols
                .Select(symbol => symbol.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList();

            return values.Count == 0
                ? "none"
                : string.Join(",", values);
        }

        private static string FormatSymbolWeights(IReadOnlyDictionary<Symbol, decimal> weights)
        {
            if (weights.Count == 0)
            {
                return "none";
            }

            return string.Join(
                ",",
                weights
                    .OrderBy(pair => pair.Key.Value, StringComparer.Ordinal)
                    .Select(pair => string.Format(CultureInfo.InvariantCulture, "{0}:{1:0.0000}", pair.Key.Value, pair.Value)));
        }

        private static string FormatGrowthCandidateScores(IReadOnlyCollection<GrowthCandidate> candidates, int maxCount)
        {
            if (candidates.Count == 0)
            {
                return "none";
            }

            return string.Join(
                ",",
                candidates
                    .OrderBy(candidate => candidate.Ticker, StringComparer.Ordinal)
                    .Take(maxCount)
                    .Select(candidate => string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}:{1:0.00}/{2:0.00}",
                        candidate.Ticker,
                        candidate.AdjustedScore,
                        candidate.FinalScore)));
        }

        private static string FormatDefensiveCandidateScores(IReadOnlyCollection<DefensiveCandidate> candidates, int maxCount)
        {
            if (candidates.Count == 0)
            {
                return "none";
            }

            return string.Join(
                ",",
                candidates
                    .OrderBy(candidate => candidate.Ticker, StringComparer.Ordinal)
                    .Take(maxCount)
                    .Select(candidate => string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}:{1:0.00}",
                        candidate.Ticker,
                        candidate.Score)));
        }

        private sealed class DiagnosticAttributionTracker
        {
            private readonly List<DiagnosticObservation> _observations = new List<DiagnosticObservation>();

            public void Record(
                DateTime date,
                decimal equity,
                RiskRegime activeRegime,
                bool preWeakGuardActive,
                bool preWeakRecoveryActive,
                bool severeCrashOverrideActive,
                decimal drawdownFromHigh,
                SleeveTargets finalSleeveTargets)
            {
                if (equity <= 0m)
                {
                    return;
                }

                var newIndex = _observations.Count;
                for (var index = 0; index < _observations.Count; index++)
                {
                    var weeksForward = newIndex - index;
                    var forwardReturn = equity / _observations[index].Equity - 1m;
                    if (weeksForward == 1)
                    {
                        _observations[index].NextReturn = forwardReturn;
                    }
                    else if (weeksForward == 4)
                    {
                        _observations[index].Forward4WeekReturn = forwardReturn;
                    }
                    else if (weeksForward == 8)
                    {
                        _observations[index].Forward8WeekReturn = forwardReturn;
                    }
                    else if (weeksForward == 12)
                    {
                        _observations[index].Forward12WeekReturn = forwardReturn;
                    }
                }

                _observations.Add(new DiagnosticObservation
                {
                    Date = date,
                    Equity = equity,
                    ActiveRegime = activeRegime,
                    PreWeakGuardActive = preWeakGuardActive,
                    PreWeakRecoveryActive = preWeakRecoveryActive,
                    SevereCrashOverrideActive = severeCrashOverrideActive,
                    DrawdownFromHigh = Math.Max(0m, drawdownFromHigh),
                    FinalGrowthTarget = finalSleeveTargets.GrowthTarget,
                    FinalDefensiveTarget = finalSleeveTargets.DefensiveTarget,
                    FinalCashTarget = finalSleeveTargets.CashTarget
                });
            }

            public string FormatSummary()
            {
                var preWeak = _observations.Where(observation => observation.PreWeakGuardActive).ToList();
                var normalPreWeak = _observations.Where(observation => observation.PreWeakGuardActive && !observation.PreWeakRecoveryActive).ToList();
                var recoveryPreWeak = _observations.Where(observation => observation.PreWeakRecoveryActive).ToList();
                var nonPreWeak = _observations.Where(observation => !observation.PreWeakGuardActive).ToList();
                var severeCrash = _observations.Where(observation => observation.SevereCrashOverrideActive).ToList();
                var weakRegime = _observations.Where(observation => observation.ActiveRegime == RiskRegime.Weak).ToList();

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "[AEGIS-DIAG-SUMMARY] Weeks={0} Start={1} End={2} PreWeakWeeks={3} NormalPreWeakWeeks={4} RecoveryPreWeakWeeks={5} NonPreWeakWeeks={6} SevereCrashWeeks={7} WeakRegimeWeeks={8} PreWeakAvgDrawdown={9} NormalPreWeakAvgDrawdown={10} RecoveryPreWeakAvgDrawdown={11} NonPreWeakAvgDrawdown={12} SevereCrashAvgDrawdown={13} PreWeakNextReturnAvg={14} NormalPreWeakNextReturnAvg={15} RecoveryPreWeakNextReturnAvg={16} NonPreWeakNextReturnAvg={17} PreWeakFwd4Avg={18} PreWeakFwd8Avg={19} PreWeakFwd12Avg={20} RecoveryPreWeakFwd4Avg={21} RecoveryPreWeakFwd8Avg={22} RecoveryPreWeakFwd12Avg={23} RecoveryPreWeakWorstNextReturn={24} RecoveryPreWeakWorstFwd4={25} PreWeakFwd4WinRate={26} RecoveryPreWeakFwd4WinRate={27} PreWeakAvgTarget=G{28}/D{29}/C{30} RecoveryPreWeakAvgTarget=G{31}/D{32}/C{33} NonPreWeakAvgTarget=G{34}/D{35}/C{36}",
                    _observations.Count,
                    _observations.Count == 0 ? "none" : _observations[0].Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    _observations.Count == 0 ? "none" : _observations[_observations.Count - 1].Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    preWeak.Count,
                    normalPreWeak.Count,
                    recoveryPreWeak.Count,
                    nonPreWeak.Count,
                    severeCrash.Count,
                    weakRegime.Count,
                    FormatDecimal(Average(preWeak, observation => observation.DrawdownFromHigh)),
                    FormatDecimal(Average(normalPreWeak, observation => observation.DrawdownFromHigh)),
                    FormatDecimal(Average(recoveryPreWeak, observation => observation.DrawdownFromHigh)),
                    FormatDecimal(Average(nonPreWeak, observation => observation.DrawdownFromHigh)),
                    FormatDecimal(Average(severeCrash, observation => observation.DrawdownFromHigh)),
                    FormatDecimal(AverageNullable(preWeak, observation => observation.NextReturn)),
                    FormatDecimal(AverageNullable(normalPreWeak, observation => observation.NextReturn)),
                    FormatDecimal(AverageNullable(recoveryPreWeak, observation => observation.NextReturn)),
                    FormatDecimal(AverageNullable(nonPreWeak, observation => observation.NextReturn)),
                    FormatDecimal(AverageNullable(preWeak, observation => observation.Forward4WeekReturn)),
                    FormatDecimal(AverageNullable(preWeak, observation => observation.Forward8WeekReturn)),
                    FormatDecimal(AverageNullable(preWeak, observation => observation.Forward12WeekReturn)),
                    FormatDecimal(AverageNullable(recoveryPreWeak, observation => observation.Forward4WeekReturn)),
                    FormatDecimal(AverageNullable(recoveryPreWeak, observation => observation.Forward8WeekReturn)),
                    FormatDecimal(AverageNullable(recoveryPreWeak, observation => observation.Forward12WeekReturn)),
                    FormatDecimal(MinNullable(recoveryPreWeak, observation => observation.NextReturn)),
                    FormatDecimal(MinNullable(recoveryPreWeak, observation => observation.Forward4WeekReturn)),
                    FormatDecimal(WinRate(preWeak, observation => observation.Forward4WeekReturn)),
                    FormatDecimal(WinRate(recoveryPreWeak, observation => observation.Forward4WeekReturn)),
                    FormatDecimal(Average(preWeak, observation => observation.FinalGrowthTarget)),
                    FormatDecimal(Average(preWeak, observation => observation.FinalDefensiveTarget)),
                    FormatDecimal(Average(preWeak, observation => observation.FinalCashTarget)),
                    FormatDecimal(Average(recoveryPreWeak, observation => observation.FinalGrowthTarget)),
                    FormatDecimal(Average(recoveryPreWeak, observation => observation.FinalDefensiveTarget)),
                    FormatDecimal(Average(recoveryPreWeak, observation => observation.FinalCashTarget)),
                    FormatDecimal(Average(nonPreWeak, observation => observation.FinalGrowthTarget)),
                    FormatDecimal(Average(nonPreWeak, observation => observation.FinalDefensiveTarget)),
                    FormatDecimal(Average(nonPreWeak, observation => observation.FinalCashTarget)));
            }

            private static decimal? Average(
                IReadOnlyCollection<DiagnosticObservation> observations,
                Func<DiagnosticObservation, decimal> selector)
            {
                return observations.Count == 0
                    ? null
                    : observations.Average(selector);
            }

            private static decimal? AverageNullable(
                IEnumerable<DiagnosticObservation> observations,
                Func<DiagnosticObservation, decimal?> selector)
            {
                var values = observations
                    .Select(selector)
                    .Where(value => value.HasValue)
                    .Select(value => value.Value)
                    .ToList();

                return values.Count == 0
                    ? null
                    : values.Average();
            }

            private static decimal? WinRate(
                IEnumerable<DiagnosticObservation> observations,
                Func<DiagnosticObservation, decimal?> selector)
            {
                var values = observations
                    .Select(selector)
                    .Where(value => value.HasValue)
                    .Select(value => value.Value)
                    .ToList();

                return values.Count == 0
                    ? null
                    : values.Count(value => value > 0m) / (decimal)values.Count;
            }

            private static decimal? MinNullable(
                IEnumerable<DiagnosticObservation> observations,
                Func<DiagnosticObservation, decimal?> selector)
            {
                var values = observations
                    .Select(selector)
                    .Where(value => value.HasValue)
                    .Select(value => value.Value)
                    .ToList();

                return values.Count == 0
                    ? null
                    : values.Min();
            }

            private static string FormatDecimal(decimal? value)
            {
                return value.HasValue
                    ? value.Value.ToString("0.0000", CultureInfo.InvariantCulture)
                    : "n/a";
            }
        }

        private sealed class DiagnosticObservation
        {
            public DateTime Date { get; set; }
            public decimal Equity { get; set; }
            public RiskRegime ActiveRegime { get; set; }
            public bool PreWeakGuardActive { get; set; }
            public bool PreWeakRecoveryActive { get; set; }
            public bool SevereCrashOverrideActive { get; set; }
            public decimal DrawdownFromHigh { get; set; }
            public decimal FinalGrowthTarget { get; set; }
            public decimal FinalDefensiveTarget { get; set; }
            public decimal FinalCashTarget { get; set; }
            public decimal? NextReturn { get; set; }
            public decimal? Forward4WeekReturn { get; set; }
            public decimal? Forward8WeekReturn { get; set; }
            public decimal? Forward12WeekReturn { get; set; }
        }
    }
}

