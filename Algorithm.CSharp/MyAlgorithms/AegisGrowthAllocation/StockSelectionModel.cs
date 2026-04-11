#region imports
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    public sealed class StockSelectionModel
    {
        public GrowthSelection SelectGrowthCandidates(
            IReadOnlyCollection<AssetSnapshot> growthSnapshots,
            RiskRegime regime,
            IReadOnlyCollection<Symbol> currentGrowthHoldings)
        {
            var allSnapshots = growthSnapshots.OrderBy(snapshot => snapshot.Ticker).ToList();
            var snapshotBySymbol = allSnapshots.ToDictionary(snapshot => snapshot.Symbol);
            var currentHoldings = new HashSet<Symbol>(currentGrowthHoldings);
            var forcedExitSymbols = new HashSet<Symbol>(
                currentHoldings.Where(symbol =>
                    !snapshotBySymbol.TryGetValue(symbol, out var snapshot) || IsForcedExit(snapshot)));

            var eligibleSnapshots = allSnapshots
                .Where(IsGrowthEligible)
                .ToList();

            var return126Percentiles = BuildPercentiles(eligibleSnapshots, snapshot => snapshot.Return126, true);
            var return63Percentiles = BuildPercentiles(eligibleSnapshots, snapshot => snapshot.Return63, true);
            var inverseVolatilityPercentiles = BuildPercentiles(eligibleSnapshots, snapshot => snapshot.Volatility63, false);
            var inverseDrawdownPercentiles = BuildPercentiles(eligibleSnapshots, snapshot => snapshot.Drawdown63, false);

            var candidates = eligibleSnapshots
                .Select(snapshot =>
                {
                    var trendDistance = NormalizePositive(snapshot.DistanceAboveSma200, StrategyConfig.TrendDistanceCap);
                    var smaSpread = NormalizePositive(snapshot.Sma50AboveSma200, StrategyConfig.SmaSpreadCap);
                    var return21 = NormalizePositive(snapshot.Return21, StrategyConfig.Return21ScoreCap);

                    var trendScore =
                        trendDistance * StrategyConfig.TrendDistanceScoreWeight +
                        smaSpread * StrategyConfig.SmaSpreadScoreWeight +
                        return21 * StrategyConfig.Return21ScoreWeight;

                    var relativeStrengthScore =
                        return126Percentiles[snapshot.Symbol] * StrategyConfig.Return126ScoreWeight +
                        return63Percentiles[snapshot.Symbol] * StrategyConfig.Return63ScoreWeight;

                    var stabilityScore =
                        inverseVolatilityPercentiles[snapshot.Symbol] * StrategyConfig.VolatilityScoreWeight +
                        inverseDrawdownPercentiles[snapshot.Symbol] * StrategyConfig.DrawdownScoreWeight;

                    var penalty = 0m;
                    if (snapshot.AtrRatio > StrategyConfig.GrowthRiskPenaltyAtrLimit)
                    {
                        penalty += 10m;
                    }

                    if (snapshot.DistanceAboveSma200 > StrategyConfig.GrowthOverextensionLimit)
                    {
                        penalty += 10m;
                    }

                    if (snapshot.Return21 < 0m)
                    {
                        penalty += 5m;
                    }

                    var finalScore = Math.Max(0m, trendScore + relativeStrengthScore + stabilityScore - penalty);
                    var isCurrentHolding = currentHoldings.Contains(snapshot.Symbol);
                    var adjustedScore = finalScore + (isCurrentHolding ? StrategyConfig.HoldStabilityBonus : 0m);

                    return new GrowthCandidate(
                        snapshot,
                        isCurrentHolding,
                        trendScore,
                        relativeStrengthScore,
                        stabilityScore,
                        penalty,
                        finalScore,
                        adjustedScore);
                })
                .OrderByDescending(candidate => candidate.AdjustedScore)
                .ThenByDescending(candidate => candidate.FinalScore)
                .ThenBy(candidate => candidate.Ticker, StringComparer.Ordinal)
                .ToList();

            return new GrowthSelection(
                allSnapshots,
                candidates,
                forcedExitSymbols,
                StrategyConfig.GrowthHoldingCountByRegime[regime]);
        }

        public DefensiveSelection SelectDefensiveCandidates(
            IReadOnlyCollection<AssetSnapshot> defensiveSnapshots,
            RiskRegime regime)
        {
            var allSnapshots = defensiveSnapshots.OrderBy(snapshot => snapshot.Ticker).ToList();
            var eligibleSnapshots = allSnapshots
                .Where(IsDefensiveEligible)
                .ToList();

            var return126Percentiles = BuildPercentiles(eligibleSnapshots, snapshot => snapshot.Return126, true);
            var inverseVolatilityPercentiles = BuildPercentiles(eligibleSnapshots, snapshot => snapshot.Volatility63, false);
            var inverseDrawdownPercentiles = BuildPercentiles(eligibleSnapshots, snapshot => snapshot.Drawdown63, false);

            var candidates = eligibleSnapshots
                .Select(snapshot =>
                {
                    var score =
                        return126Percentiles[snapshot.Symbol] * StrategyConfig.DefensiveReturn126Weight +
                        inverseVolatilityPercentiles[snapshot.Symbol] * StrategyConfig.DefensiveVolatilityWeight +
                        inverseDrawdownPercentiles[snapshot.Symbol] * StrategyConfig.DefensiveDrawdownWeight;

                    return new DefensiveCandidate(snapshot, score);
                })
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Ticker, StringComparer.Ordinal)
                .ToList();

            return new DefensiveSelection(
                allSnapshots,
                candidates,
                StrategyConfig.DefensiveHoldingCountByRegime[regime]);
        }

        private static bool IsGrowthEligible(AssetSnapshot snapshot)
        {
            return snapshot.IsDataReady &&
                   snapshot.Close > snapshot.Sma200 &&
                   snapshot.Sma50 >= snapshot.Sma200 &&
                   snapshot.Return63 > 0m &&
                   snapshot.AtrRatio <= StrategyConfig.GrowthAtrEligibilityLimit;
        }

        private static bool IsForcedExit(AssetSnapshot snapshot)
        {
            return !snapshot.IsDataReady ||
                   snapshot.Close < snapshot.Sma200 ||
                   snapshot.Sma50 < snapshot.Sma200 ||
                   snapshot.AtrRatio > StrategyConfig.GrowthAtrForcedExitLimit;
        }

        private static bool IsDefensiveEligible(AssetSnapshot snapshot)
        {
            return snapshot.IsDataReady &&
                   (snapshot.IsSgov || snapshot.Close > snapshot.Sma200);
        }

        private static Dictionary<Symbol, decimal> BuildPercentiles(
            IReadOnlyList<AssetSnapshot> snapshots,
            Func<AssetSnapshot, decimal> selector,
            bool descending)
        {
            var percentiles = new Dictionary<Symbol, decimal>();
            if (snapshots.Count == 0)
            {
                return percentiles;
            }

            var ordered = descending
                ? snapshots.OrderByDescending(selector).ThenBy(snapshot => snapshot.Ticker, StringComparer.Ordinal).ToList()
                : snapshots.OrderBy(selector).ThenBy(snapshot => snapshot.Ticker, StringComparer.Ordinal).ToList();

            if (ordered.Count == 1)
            {
                percentiles[ordered[0].Symbol] = 1m;
                return percentiles;
            }

            for (var index = 0; index < ordered.Count; index++)
            {
                percentiles[ordered[index].Symbol] = 1m - (decimal)index / (ordered.Count - 1);
            }

            return percentiles;
        }

        private static decimal NormalizePositive(decimal value, decimal cap)
        {
            if (cap <= 0m)
            {
                return 0m;
            }

            return Math.Clamp(value, 0m, cap) / cap;
        }
    }

    public sealed class AssetSnapshot
    {
        public AssetSnapshot(
            Symbol symbol,
            string ticker,
            bool isGrowth,
            bool isDefensive,
            bool isSgov,
            bool isDataReady,
            decimal close,
            decimal sma50,
            decimal sma200,
            decimal atr20,
            decimal return21,
            decimal return63,
            decimal return126,
            decimal volatility63,
            decimal drawdown63)
        {
            Symbol = symbol;
            Ticker = ticker;
            IsGrowth = isGrowth;
            IsDefensive = isDefensive;
            IsSgov = isSgov;
            IsDataReady = isDataReady;
            Close = close;
            Sma50 = sma50;
            Sma200 = sma200;
            Atr20 = atr20;
            Return21 = return21;
            Return63 = return63;
            Return126 = return126;
            Volatility63 = volatility63;
            Drawdown63 = drawdown63;
        }

        public Symbol Symbol { get; }
        public string Ticker { get; }
        public bool IsGrowth { get; }
        public bool IsDefensive { get; }
        public bool IsSgov { get; }
        public bool IsDataReady { get; }
        public decimal Close { get; }
        public decimal Sma50 { get; }
        public decimal Sma200 { get; }
        public decimal Atr20 { get; }
        public decimal Return21 { get; }
        public decimal Return63 { get; }
        public decimal Return126 { get; }
        public decimal Volatility63 { get; }
        public decimal Drawdown63 { get; }

        public decimal AtrRatio => Close > 0m ? Atr20 / Close : 0m;
        public decimal DistanceAboveSma200 => Sma200 > 0m ? (Close / Sma200) - 1m : 0m;
        public decimal Sma50AboveSma200 => Sma200 > 0m ? (Sma50 / Sma200) - 1m : 0m;
    }

    public sealed class GrowthCandidate
    {
        public GrowthCandidate(
            AssetSnapshot snapshot,
            bool isCurrentHolding,
            decimal trendScore,
            decimal relativeStrengthScore,
            decimal stabilityScore,
            decimal penalty,
            decimal finalScore,
            decimal adjustedScore)
        {
            Snapshot = snapshot;
            IsCurrentHolding = isCurrentHolding;
            TrendScore = trendScore;
            RelativeStrengthScore = relativeStrengthScore;
            StabilityScore = stabilityScore;
            Penalty = penalty;
            FinalScore = finalScore;
            AdjustedScore = adjustedScore;
        }

        public AssetSnapshot Snapshot { get; }
        public bool IsCurrentHolding { get; }
        public decimal TrendScore { get; }
        public decimal RelativeStrengthScore { get; }
        public decimal StabilityScore { get; }
        public decimal Penalty { get; }
        public decimal FinalScore { get; }
        public decimal AdjustedScore { get; }

        public Symbol Symbol => Snapshot.Symbol;
        public string Ticker => Snapshot.Ticker;
    }

    public sealed class DefensiveCandidate
    {
        public DefensiveCandidate(AssetSnapshot snapshot, decimal score)
        {
            Snapshot = snapshot;
            Score = score;
        }

        public AssetSnapshot Snapshot { get; }
        public decimal Score { get; }

        public Symbol Symbol => Snapshot.Symbol;
        public string Ticker => Snapshot.Ticker;
    }

    public sealed class GrowthSelection
    {
        public GrowthSelection(
            IReadOnlyList<AssetSnapshot> allSnapshots,
            IReadOnlyList<GrowthCandidate> rankedCandidates,
            IReadOnlyCollection<Symbol> forcedExitSymbols,
            int targetHoldingCount)
        {
            AllSnapshots = allSnapshots;
            RankedCandidates = rankedCandidates;
            ForcedExitSymbols = forcedExitSymbols;
            TargetHoldingCount = targetHoldingCount;
            CandidatesBySymbol = rankedCandidates.ToDictionary(candidate => candidate.Symbol);
            SnapshotBySymbol = allSnapshots.ToDictionary(snapshot => snapshot.Symbol);
        }

        public IReadOnlyList<AssetSnapshot> AllSnapshots { get; }
        public IReadOnlyList<GrowthCandidate> RankedCandidates { get; }
        public IReadOnlyCollection<Symbol> ForcedExitSymbols { get; }
        public int TargetHoldingCount { get; }
        public IReadOnlyDictionary<Symbol, GrowthCandidate> CandidatesBySymbol { get; }
        public IReadOnlyDictionary<Symbol, AssetSnapshot> SnapshotBySymbol { get; }
    }

    public sealed class DefensiveSelection
    {
        public DefensiveSelection(
            IReadOnlyList<AssetSnapshot> allSnapshots,
            IReadOnlyList<DefensiveCandidate> rankedCandidates,
            int targetHoldingCount)
        {
            AllSnapshots = allSnapshots;
            RankedCandidates = rankedCandidates;
            TargetHoldingCount = targetHoldingCount;
            SnapshotBySymbol = allSnapshots.ToDictionary(snapshot => snapshot.Symbol);
        }

        public IReadOnlyList<AssetSnapshot> AllSnapshots { get; }
        public IReadOnlyList<DefensiveCandidate> RankedCandidates { get; }
        public int TargetHoldingCount { get; }
        public IReadOnlyDictionary<Symbol, AssetSnapshot> SnapshotBySymbol { get; }
    }
}
