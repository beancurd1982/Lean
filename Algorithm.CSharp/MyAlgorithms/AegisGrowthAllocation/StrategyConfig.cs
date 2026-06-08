#region imports
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    public static class StrategyConfig
    {
        public const string MarketTicker = "SPY";
        public const string StressTicker = "VIX";
        public const int WarmupTradingDays = 252;
        public const string UndeployedReserveParameter = "undeployed-reserve";
        public const string BacktestStartParameter = "backtest-start";
        public const string BacktestEndParameter = "backtest-end";
        public const string CrisisDiagnosticsParameter = "crisis-diagnostics";
        public const string WeakStressOverlayParameter = "weak-stress-overlay-enabled";
        public const string PreWeakGuardParameter = "pre-weak-guard-enabled";
        public const string PreWeakGuardDrawdownThresholdParameter = "pre-weak-dd-threshold";
        public const string SevereCrashOverrideParameter = "severe-crash-override-enabled";
        public const string SevereCrashOverrideDrawdownThresholdParameter = "sev-crash-dd-entry";
        public const string SevereCrashOverrideExitDrawdownThresholdParameter = "sev-crash-dd-exit";
        public const string SevereCrashOverrideRecoveryConfirmationWeeksParameter = "sev-crash-recovery-wks";
        public const string WeakStressThresholdParameter = "weak-stress-threshold";
        public const string SevereStressGapParameter = "severe-stress-gap";
        public const string FavorableBreadthThresholdParameter = "favorable-breadth-threshold";
        public const string UpgradeConfirmationWeeksParameter = "upgrade-confirmation-weeks";
        public const string ReplacementScoreGapParameter = "replacement-score-gap";
        public const string HoldStabilityBonusParameter = "hold-stability-bonus";
        public const string GrowthAtrEligibilityLimitParameter = "growth-atr-eligibility-limit";
        public const string RebalanceToleranceBandScaleParameter = "tolerance-band-scale";
        public const string PreWeakGrowthTargetParameter = "pre-weak-growth-target";
        public const string PreWeakDefensiveTargetParameter = "pre-weak-def-target";
        public const string WeakGrowthTargetParameter = "weak-growth-target";
        public const string LiveStateKey = "AegisGrowthAllocation_LiveState_V2";
        public const int LiveStateSchemaVersion = 2;
        public const decimal LiveStateQuantityTolerance = 0.0001m;
        public const string AlgorithmVersion = "AegisGrowthAllocation-2026-05-23-live-state-diagnostics-v2";
        public const string SourceRevision = "4c59639af";
        public static readonly DateTime DefaultBacktestStartDate = new DateTime(2018, 1, 1);
        public static readonly TimeSpan WeeklyDecisionTime = new TimeSpan(10, 0, 0);
        public const int CloseWindowSize = 252;
        public const int Return21Period = 21;
        public const int Return63Period = 63;
        public const int Return126Period = 126;
        public const int VolatilityLookbackDays = 63;
        public const int DrawdownLookbackDays = 63;
        public const int VixAverageWindow = 5;
        public const int SpySmaLookbackWindowSize = TrendSlopeLookbackDays + 1;

        public static readonly IReadOnlyList<string> CoreGrowthTickers = new[]
        {
            "MSFT", "NVDA", "AMZN", "GOOGL", "META", "AVGO", "AAPL", "COST"
        };

        public static readonly IReadOnlyList<string> SupplementalGrowthTickers = new[]
        {
            "LLY", "NFLX", "TSLA"
        };

        public static readonly IReadOnlyList<string> DefensiveTickers = new[]
        {
            "SCHD", "VIG", "XLV", "XLU", "USMV", "SGOV", "JNJ", "PG", "DUK"
        };

        public static readonly IReadOnlyList<string> GrowthTickers =
            CoreGrowthTickers.Concat(SupplementalGrowthTickers).ToArray();

        public const decimal TrendUpperBuffer = 0.02m;
        public const decimal TrendLowerBuffer = 0.02m;
        public const int TrendSlopeLookbackDays = 20;

        public const decimal DefaultFavorableBreadthThreshold = 0.85m;
        public const decimal WeakBreadthThreshold = 0.40m;

        public const decimal FavorableStressThreshold = 18m;
        public const decimal DefaultWeakStressThreshold = 33m;
        public const decimal DefaultSevereStressGap = 4m;
        public const decimal DefaultSevereStressThreshold = DefaultWeakStressThreshold + DefaultSevereStressGap;
        public const decimal MaxWeakStressThreshold = 40m;
        public const decimal MaxSevereStressThreshold = 45m;
        public const bool DefaultPreWeakGuardEnabled = true;
        public const decimal DefaultPreWeakGuardDrawdownThreshold = 0.04m;
        public const decimal DefaultPreWeakGrowthTarget = 0.12m;
        public const decimal DefaultPreWeakDefensiveTarget = 0.30m;
        public const decimal DefaultWeakGrowthTarget = 0.10m;
        public const decimal WeakDefensiveTarget = 0.40m;
        public const decimal DefaultSevereCrashOverrideDrawdownThreshold = 0.10m;
        public const decimal DefaultSevereCrashOverrideExitDrawdownThreshold = 0.07m;

        public const int DefaultUpgradeConfirmationWeeks = 1;
        public const int DefaultSevereCrashOverrideRecoveryConfirmationWeeks = 2;

        public static decimal FavorableBreadthThreshold { get; private set; } = DefaultFavorableBreadthThreshold;
        public static decimal WeakStressThreshold { get; private set; } = DefaultWeakStressThreshold;
        public static decimal SevereStressGap { get; private set; } = DefaultSevereStressGap;
        public static decimal SevereStressThreshold { get; private set; } = DefaultSevereStressThreshold;
        public static int UpgradeConfirmationWeeks { get; private set; } = DefaultUpgradeConfirmationWeeks;

        private static readonly IReadOnlyDictionary<RiskRegime, SleeveTargets> DefaultSleeveTargetsByRegime =
            new Dictionary<RiskRegime, SleeveTargets>
            {
                [RiskRegime.Favorable] = new SleeveTargets(
                    growthTarget: 0.65m,
                    defensiveTarget: 0.20m,
                    cashTarget: 0.15m,
                    growthMin: 0.60m,
                    growthMax: 0.70m,
                    defensiveMin: 0.15m,
                    defensiveMax: 0.25m,
                    cashMin: 0.10m,
                    cashMax: 0.20m),
                [RiskRegime.Neutral] = new SleeveTargets(
                    growthTarget: 0.45m,
                    defensiveTarget: 0.30m,
                    cashTarget: 0.25m,
                    growthMin: 0.40m,
                    growthMax: 0.50m,
                    defensiveMin: 0.25m,
                    defensiveMax: 0.35m,
                    cashMin: 0.20m,
                    cashMax: 0.30m),
                [RiskRegime.Weak] = new SleeveTargets(
                    growthTarget: DefaultWeakGrowthTarget,
                    defensiveTarget: WeakDefensiveTarget,
                    cashTarget: 1m - DefaultWeakGrowthTarget - WeakDefensiveTarget,
                    growthMin: 0.05m,
                    growthMax: 0.15m,
                    defensiveMin: 0.35m,
                    defensiveMax: 0.45m,
                    cashMin: 0.40m,
                    cashMax: 0.55m)
            };

        private static SleeveTargets _weakSleeveTargets = DefaultSleeveTargetsByRegime[RiskRegime.Weak];

        public static IReadOnlyDictionary<RiskRegime, SleeveTargets> SleeveTargetsByRegime =>
            new Dictionary<RiskRegime, SleeveTargets>
            {
                [RiskRegime.Favorable] = DefaultSleeveTargetsByRegime[RiskRegime.Favorable],
                [RiskRegime.Neutral] = DefaultSleeveTargetsByRegime[RiskRegime.Neutral],
                [RiskRegime.Weak] = _weakSleeveTargets
            };

        public static readonly SleeveTargets WeakStressOverlaySleeveTargets = new SleeveTargets(
            growthTarget: 0.00m,
            defensiveTarget: 0.20m,
            cashTarget: 0.80m,
            growthMin: 0.00m,
            growthMax: 0.05m,
            defensiveMin: 0.00m,
            defensiveMax: 0.25m,
            cashMin: 0.75m,
            cashMax: 1.00m);

        private static SleeveTargets _preWeakGuardSleeveTargets = BuildPreWeakGuardSleeveTargets(
            DefaultPreWeakGrowthTarget,
            DefaultPreWeakDefensiveTarget);

        public static SleeveTargets PreWeakGuardSleeveTargets => _preWeakGuardSleeveTargets;

        public static readonly SleeveTargets SevereCrashOverrideSleeveTargets = new SleeveTargets(
            growthTarget: 0.00m,
            defensiveTarget: 0.20m,
            cashTarget: 0.80m,
            growthMin: 0.00m,
            growthMax: 0.05m,
            defensiveMin: 0.00m,
            defensiveMax: 0.25m,
            cashMin: 0.75m,
            cashMax: 1.00m);

        public static readonly IReadOnlyDictionary<RiskRegime, int> GrowthHoldingCountByRegime =
            new Dictionary<RiskRegime, int>
            {
                [RiskRegime.Favorable] = 6,
                [RiskRegime.Neutral] = 4,
                [RiskRegime.Weak] = 1
            };

        public static readonly IReadOnlyDictionary<RiskRegime, int> DefensiveHoldingCountByRegime =
            new Dictionary<RiskRegime, int>
            {
                [RiskRegime.Favorable] = 2,
                [RiskRegime.Neutral] = 3,
                [RiskRegime.Weak] = 3
            };

        public static readonly IReadOnlyDictionary<RiskRegime, decimal> ReserveReleaseRateByRegime =
            new Dictionary<RiskRegime, decimal>
            {
                [RiskRegime.Favorable] = 0.33m,
                [RiskRegime.Neutral] = 0.15m,
                [RiskRegime.Weak] = 0.00m
            };

        public const decimal DefaultGrowthAtrEligibilityLimit = 0.06m;
        public const decimal DefaultRebalanceToleranceBandScale = 1m;
        public const decimal GrowthAtrForcedExitLimit = 0.07m;
        public const decimal GrowthRiskPenaltyAtrLimit = 0.05m;
        public const decimal GrowthOverextensionLimit = 0.20m;
        public const decimal TrendDistanceCap = 0.15m;
        public const decimal SmaSpreadCap = 0.10m;
        public const decimal Return21ScoreCap = 0.10m;
        public const decimal TrendDistanceScoreWeight = 20m;
        public const decimal SmaSpreadScoreWeight = 15m;
        public const decimal Return21ScoreWeight = 10m;
        public const decimal Return126ScoreWeight = 20m;
        public const decimal Return63ScoreWeight = 15m;
        public const decimal VolatilityScoreWeight = 10m;
        public const decimal DrawdownScoreWeight = 10m;
        public const decimal DefensiveReturn126Weight = 0.50m;
        public const decimal DefensiveVolatilityWeight = 0.30m;
        public const decimal DefensiveDrawdownWeight = 0.20m;
        public const decimal SingleGrowthWeightCap = 0.12m;
        public const decimal SmallTradeThreshold = 0.005m;
        public const int MaxOptimizationReplacementsPerWeek = 1;
        public const int MaxRankingDrivenEntriesPerWeek = 2;
        public const decimal DefaultReplacementScoreGap = 10m;
        public const decimal DefaultHoldStabilityBonus = 2m;

        public static decimal GrowthAtrEligibilityLimit { get; private set; } = DefaultGrowthAtrEligibilityLimit;
        public static decimal ReplacementScoreGap { get; private set; } = DefaultReplacementScoreGap;
        public static decimal HoldStabilityBonus { get; private set; } = DefaultHoldStabilityBonus;
        public static decimal RebalanceToleranceBandScale { get; private set; } = DefaultRebalanceToleranceBandScale;
        public static decimal PreWeakGrowthTarget { get; private set; } = DefaultPreWeakGrowthTarget;
        public static decimal PreWeakDefensiveTarget { get; private set; } = DefaultPreWeakDefensiveTarget;
        public static decimal WeakGrowthTarget { get; private set; } = DefaultWeakGrowthTarget;

        public static SleeveTargets GetSleeveTargets(RiskRegime regime)
        {
            return regime == RiskRegime.Weak
                ? _weakSleeveTargets
                : DefaultSleeveTargetsByRegime[regime];
        }

        public static void ResetRuntimeParameters()
        {
            FavorableBreadthThreshold = DefaultFavorableBreadthThreshold;
            WeakStressThreshold = DefaultWeakStressThreshold;
            SevereStressGap = DefaultSevereStressGap;
            SevereStressThreshold = DefaultSevereStressThreshold;
            UpgradeConfirmationWeeks = DefaultUpgradeConfirmationWeeks;
            GrowthAtrEligibilityLimit = DefaultGrowthAtrEligibilityLimit;
            ReplacementScoreGap = DefaultReplacementScoreGap;
            HoldStabilityBonus = DefaultHoldStabilityBonus;
            RebalanceToleranceBandScale = DefaultRebalanceToleranceBandScale;
            PreWeakGrowthTarget = DefaultPreWeakGrowthTarget;
            PreWeakDefensiveTarget = DefaultPreWeakDefensiveTarget;
            WeakGrowthTarget = DefaultWeakGrowthTarget;
            _preWeakGuardSleeveTargets = BuildPreWeakGuardSleeveTargets(
                DefaultPreWeakGrowthTarget,
                DefaultPreWeakDefensiveTarget);
            _weakSleeveTargets = BuildWeakSleeveTargets(DefaultWeakGrowthTarget);
        }

        public static void ConfigureRuntimeParameters(
            decimal favorableBreadthThreshold,
            decimal weakStressThreshold,
            decimal severeStressGap,
            int upgradeConfirmationWeeks,
            decimal growthAtrEligibilityLimit,
            decimal replacementScoreGap,
            decimal holdStabilityBonus,
            decimal rebalanceToleranceBandScale,
            decimal preWeakGrowthTarget,
            decimal preWeakDefensiveTarget,
            decimal weakGrowthTarget)
        {
            FavorableBreadthThreshold = favorableBreadthThreshold;
            WeakStressThreshold = weakStressThreshold;
            SevereStressGap = severeStressGap;
            SevereStressThreshold = weakStressThreshold + severeStressGap;
            UpgradeConfirmationWeeks = upgradeConfirmationWeeks;
            GrowthAtrEligibilityLimit = growthAtrEligibilityLimit;
            ReplacementScoreGap = replacementScoreGap;
            HoldStabilityBonus = holdStabilityBonus;
            RebalanceToleranceBandScale = rebalanceToleranceBandScale;
            PreWeakGrowthTarget = preWeakGrowthTarget;
            PreWeakDefensiveTarget = preWeakDefensiveTarget;
            WeakGrowthTarget = weakGrowthTarget;
            _preWeakGuardSleeveTargets = BuildPreWeakGuardSleeveTargets(
                preWeakGrowthTarget,
                preWeakDefensiveTarget);
            _weakSleeveTargets = BuildWeakSleeveTargets(weakGrowthTarget);
        }

        public static (decimal PreWeakGrowthTarget, decimal PreWeakDefensiveTarget, decimal WeakGrowthTarget) ParseSleeveTargetParameters(
            Func<string, string> getParameter,
            Action<string> debug)
        {
            var preWeakGrowthValid = TryParseOptionalDecimalParameter(
                getParameter,
                debug,
                PreWeakGrowthTargetParameter,
                DefaultPreWeakGrowthTarget,
                value => value >= 0m && value <= 0.50m,
                out var preWeakGrowthTarget);
            var preWeakDefensiveValid = TryParseOptionalDecimalParameter(
                getParameter,
                debug,
                PreWeakDefensiveTargetParameter,
                DefaultPreWeakDefensiveTarget,
                value => value >= 0m && value <= 0.60m,
                out var preWeakDefensiveTarget);
            var weakGrowthValid = TryParseOptionalDecimalParameter(
                getParameter,
                debug,
                WeakGrowthTargetParameter,
                DefaultWeakGrowthTarget,
                value => value >= 0m && value <= 0.30m,
                out var weakGrowthTarget);

            if (!preWeakGrowthValid ||
                !preWeakDefensiveValid ||
                !weakGrowthValid ||
                preWeakGrowthTarget + preWeakDefensiveTarget >= 1m ||
                weakGrowthTarget + WeakDefensiveTarget >= 1m)
            {
                debug(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[AEGIS] Invalid sleeve target parameters pre-weak-growth-target={0} pre-weak-def-target={1} weak-growth-target={2}. Using defaults preWeakGrowth={3} preWeakDef={4} weakGrowth={5}.",
                        preWeakGrowthTarget,
                        preWeakDefensiveTarget,
                        weakGrowthTarget,
                        DefaultPreWeakGrowthTarget,
                        DefaultPreWeakDefensiveTarget,
                        DefaultWeakGrowthTarget));
                return (DefaultPreWeakGrowthTarget, DefaultPreWeakDefensiveTarget, DefaultWeakGrowthTarget);
            }

            return (preWeakGrowthTarget, preWeakDefensiveTarget, weakGrowthTarget);
        }

        public static (decimal WeakStressThreshold, decimal SevereStressGap) ParseStressBandParameters(
            Func<string, string> getParameter,
            Action<string> debug)
        {
            var weakStressValid = TryParseOptionalDecimalParameter(
                getParameter,
                debug,
                WeakStressThresholdParameter,
                DefaultWeakStressThreshold,
                value => value > FavorableStressThreshold && value <= MaxWeakStressThreshold,
                out var weakStressThreshold);
            var severeStressGapValid = TryParseOptionalDecimalParameter(
                getParameter,
                debug,
                SevereStressGapParameter,
                DefaultSevereStressGap,
                value => value >= 2m && value <= 8m,
                out var severeStressGap);

            if (!weakStressValid || !severeStressGapValid)
            {
                return (DefaultWeakStressThreshold, DefaultSevereStressGap);
            }

            var computedSevereStressThreshold = weakStressThreshold + severeStressGap;
            if (computedSevereStressThreshold > MaxSevereStressThreshold)
            {
                debug(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[AEGIS] Invalid stress band weak-stress-threshold={0} severe-stress-gap={1} computed-severe-stress-threshold={2}. Using defaults weak={3} severe-gap={4}.",
                        weakStressThreshold,
                        severeStressGap,
                        computedSevereStressThreshold,
                        DefaultWeakStressThreshold,
                        DefaultSevereStressGap));
                return (DefaultWeakStressThreshold, DefaultSevereStressGap);
            }

            return (weakStressThreshold, severeStressGap);
        }

        private static bool TryParseOptionalDecimalParameter(
            Func<string, string> getParameter,
            Action<string> debug,
            string name,
            decimal defaultValue,
            Func<decimal, bool> validator,
            out decimal value)
        {
            var raw = getParameter(name);
            if (string.IsNullOrWhiteSpace(raw))
            {
                value = defaultValue;
                return true;
            }

            if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                debug($"[AEGIS] Invalid decimal parameter {name}={raw}. Using default {defaultValue.ToString(CultureInfo.InvariantCulture)}.");
                value = defaultValue;
                return false;
            }

            if (!validator(value))
            {
                debug($"[AEGIS] Out-of-range decimal parameter {name}={value.ToString(CultureInfo.InvariantCulture)}. Using default {defaultValue.ToString(CultureInfo.InvariantCulture)}.");
                value = defaultValue;
                return false;
            }

            return true;
        }

        private static SleeveTargets BuildPreWeakGuardSleeveTargets(decimal growthTarget, decimal defensiveTarget)
        {
            var cashTarget = 1m - growthTarget - defensiveTarget;

            return new SleeveTargets(
                growthTarget: growthTarget,
                defensiveTarget: defensiveTarget,
                cashTarget: cashTarget,
                growthMin: Math.Max(0m, growthTarget - 0.04m),
                growthMax: Math.Min(1m, growthTarget + 0.06m),
                defensiveMin: Math.Max(0m, defensiveTarget - 0.05m),
                defensiveMax: Math.Min(1m, defensiveTarget + 0.05m),
                cashMin: Math.Max(0m, cashTarget - 0.06m),
                cashMax: Math.Min(1m, cashTarget + 0.09m));
        }

        private static SleeveTargets BuildWeakSleeveTargets(decimal growthTarget)
        {
            var cashTarget = 1m - growthTarget - WeakDefensiveTarget;

            return new SleeveTargets(
                growthTarget: growthTarget,
                defensiveTarget: WeakDefensiveTarget,
                cashTarget: cashTarget,
                growthMin: Math.Max(0m, growthTarget - 0.05m),
                growthMax: Math.Min(1m, growthTarget + 0.05m),
                defensiveMin: 0.35m,
                defensiveMax: 0.45m,
                cashMin: Math.Max(0m, cashTarget - 0.10m),
                cashMax: Math.Min(1m, cashTarget + 0.05m));
        }
    }

    public sealed class SleeveTargets
    {
        public SleeveTargets(
            decimal growthTarget,
            decimal defensiveTarget,
            decimal cashTarget,
            decimal growthMin,
            decimal growthMax,
            decimal defensiveMin,
            decimal defensiveMax,
            decimal cashMin,
            decimal cashMax)
        {
            GrowthTarget = growthTarget;
            DefensiveTarget = defensiveTarget;
            CashTarget = cashTarget;
            GrowthMin = growthMin;
            GrowthMax = growthMax;
            DefensiveMin = defensiveMin;
            DefensiveMax = defensiveMax;
            CashMin = cashMin;
            CashMax = cashMax;
        }

        public decimal GrowthTarget { get; }
        public decimal DefensiveTarget { get; }
        public decimal CashTarget { get; }
        public decimal GrowthMin { get; }
        public decimal GrowthMax { get; }
        public decimal DefensiveMin { get; }
        public decimal DefensiveMax { get; }
        public decimal CashMin { get; }
        public decimal CashMax { get; }

        public bool IsGrowthWithinBand(decimal weight)
        {
            return IsWithinBand(weight, GrowthTarget, GrowthMin, GrowthMax);
        }

        public bool IsDefensiveWithinBand(decimal weight)
        {
            return IsWithinBand(weight, DefensiveTarget, DefensiveMin, DefensiveMax);
        }

        public bool IsCashWithinBand(decimal weight)
        {
            return IsWithinBand(weight, CashTarget, CashMin, CashMax);
        }

        private static bool IsWithinBand(decimal weight, decimal target, decimal minimum, decimal maximum)
        {
            var scale = StrategyConfig.RebalanceToleranceBandScale;
            var scaledMinimum = target - ((target - minimum) * scale);
            var scaledMaximum = target + ((maximum - target) * scale);

            return weight >= scaledMinimum && weight <= scaledMaximum;
        }
    }
}
