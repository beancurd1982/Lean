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
        public static readonly TimeSpan WeeklyDecisionTime = new TimeSpan(10, 0, 0);

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

        public const decimal FavorableBreadthThreshold = 0.70m;
        public const decimal WeakBreadthThreshold = 0.40m;

        public const decimal FavorableStressThreshold = 18m;
        public const decimal WeakStressThreshold = 25m;
        public const decimal SevereStressThreshold = 30m;

        public const int UpgradeConfirmationWeeks = 2;

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

        public const decimal GrowthAtrEligibilityLimit = 0.06m;
        public const decimal GrowthAtrForcedExitLimit = 0.07m;
        public const decimal GrowthRiskPenaltyAtrLimit = 0.05m;
        public const decimal GrowthOverextensionLimit = 0.20m;
        public const decimal SingleGrowthWeightCap = 0.12m;
        public const decimal SmallTradeThreshold = 0.005m;
        public const int MaxOptimizationReplacementsPerWeek = 1;
        public const int MaxRankingDrivenEntriesPerWeek = 2;
        public const decimal ReplacementScoreGap = 10m;
        public const decimal HoldStabilityBonus = 5m;

        public static SleeveTargets GetSleeveTargets(RiskRegime regime)
        {
            return SleeveTargetsByRegime[regime];
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
    }
}
