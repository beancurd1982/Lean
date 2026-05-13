#region imports
using System;
using System.Collections.Generic;
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
        public const string PreWeakGuardDrawdownThresholdParameter = "pre-weak-guard-drawdown-threshold";
        public const string SevereCrashOverrideParameter = "severe-crash-override-enabled";
        public const string SevereCrashOverrideDrawdownThresholdParameter = "sev-crash-dd-entry";
        public const string SevereCrashOverrideExitDrawdownThresholdParameter = "sev-crash-dd-exit";
        public const string SevereCrashOverrideRecoveryConfirmationWeeksParameter = "sev-crash-recovery-wks";
        public const string WeakStressThresholdParameter = "weak-stress-threshold";
        public const string FavorableBreadthThresholdParameter = "favorable-breadth-threshold";
        public const string UpgradeConfirmationWeeksParameter = "upgrade-confirmation-weeks";
        public const string ReplacementScoreGapParameter = "replacement-score-gap";
        public const string HoldStabilityBonusParameter = "hold-stability-bonus";
        public const string GrowthAtrEligibilityLimitParameter = "growth-atr-eligibility-limit";
        public const string RebalanceToleranceBandScaleParameter = "tolerance-band-scale";
        public const string LiveStateKey = "AegisGrowthAllocation_LiveState_V1";
        public const int LiveStateSchemaVersion = 1;
        public const decimal LiveStateQuantityTolerance = 0.0001m;
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

        public const decimal DefaultFavorableBreadthThreshold = 0.75m;
        public const decimal WeakBreadthThreshold = 0.40m;

        public const decimal FavorableStressThreshold = 18m;
        public const decimal DefaultWeakStressThreshold = 27m;
        public const decimal SevereStressThreshold = 30m;
        public const decimal DefaultPreWeakGuardDrawdownThreshold = 0.05m;
        public const decimal DefaultSevereCrashOverrideDrawdownThreshold = 0.10m;
        public const decimal DefaultSevereCrashOverrideExitDrawdownThreshold = 0.07m;

        public const int DefaultUpgradeConfirmationWeeks = 1;
        public const int DefaultSevereCrashOverrideRecoveryConfirmationWeeks = 2;

        public static decimal FavorableBreadthThreshold { get; private set; } = DefaultFavorableBreadthThreshold;
        public static decimal WeakStressThreshold { get; private set; } = DefaultWeakStressThreshold;
        public static int UpgradeConfirmationWeeks { get; private set; } = DefaultUpgradeConfirmationWeeks;

        public static readonly IReadOnlyDictionary<RiskRegime, SleeveTargets> SleeveTargetsByRegime =
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
                    growthTarget: 0.10m,
                    defensiveTarget: 0.40m,
                    cashTarget: 0.50m,
                    growthMin: 0.05m,
                    growthMax: 0.15m,
                    defensiveMin: 0.35m,
                    defensiveMax: 0.45m,
                    cashMin: 0.40m,
                    cashMax: 0.55m)
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

        public static readonly SleeveTargets PreWeakGuardSleeveTargets = new SleeveTargets(
            growthTarget: 0.24m,
            defensiveTarget: 0.30m,
            cashTarget: 0.46m,
            growthMin: 0.20m,
            growthMax: 0.30m,
            defensiveMin: 0.25m,
            defensiveMax: 0.35m,
            cashMin: 0.40m,
            cashMax: 0.55m);

        public static readonly SleeveTargets SevereCrashOverrideSleeveTargets = new SleeveTargets(
            growthTarget: 0.05m,
            defensiveTarget: 0.35m,
            cashTarget: 0.60m,
            growthMin: 0.00m,
            growthMax: 0.10m,
            defensiveMin: 0.30m,
            defensiveMax: 0.40m,
            cashMin: 0.55m,
            cashMax: 0.65m);

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

        public static SleeveTargets GetSleeveTargets(RiskRegime regime)
        {
            return SleeveTargetsByRegime[regime];
        }

        public static void ResetRuntimeParameters()
        {
            FavorableBreadthThreshold = DefaultFavorableBreadthThreshold;
            WeakStressThreshold = DefaultWeakStressThreshold;
            UpgradeConfirmationWeeks = DefaultUpgradeConfirmationWeeks;
            GrowthAtrEligibilityLimit = DefaultGrowthAtrEligibilityLimit;
            ReplacementScoreGap = DefaultReplacementScoreGap;
            HoldStabilityBonus = DefaultHoldStabilityBonus;
            RebalanceToleranceBandScale = DefaultRebalanceToleranceBandScale;
        }

        public static void ConfigureRuntimeParameters(
            decimal favorableBreadthThreshold,
            decimal weakStressThreshold,
            int upgradeConfirmationWeeks,
            decimal growthAtrEligibilityLimit,
            decimal replacementScoreGap,
            decimal holdStabilityBonus,
            decimal rebalanceToleranceBandScale)
        {
            FavorableBreadthThreshold = favorableBreadthThreshold;
            WeakStressThreshold = weakStressThreshold;
            UpgradeConfirmationWeeks = upgradeConfirmationWeeks;
            GrowthAtrEligibilityLimit = growthAtrEligibilityLimit;
            ReplacementScoreGap = replacementScoreGap;
            HoldStabilityBonus = holdStabilityBonus;
            RebalanceToleranceBandScale = rebalanceToleranceBandScale;
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
