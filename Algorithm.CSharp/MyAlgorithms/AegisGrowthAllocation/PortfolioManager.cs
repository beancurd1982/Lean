#region imports
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
#endregion

namespace QuantConnect.Algorithm.CSharp
{
    public sealed class PortfolioManager
    {
        public PortfolioPlan BuildPlan(
            RiskRegime previousRegime,
            RiskRegime activeRegime,
            GrowthSelection growthSelection,
            DefensiveSelection defensiveSelection,
            IReadOnlyDictionary<Symbol, decimal> currentWeights,
            decimal undeployedCapitalReserve)
        {
            var sleeveTargets = StrategyConfig.GetSleeveTargets(activeRegime);
            var growthUniverse = new HashSet<Symbol>(growthSelection.AllSnapshots.Select(snapshot => snapshot.Symbol));
            var defensiveUniverse = new HashSet<Symbol>(defensiveSelection.AllSnapshots.Select(snapshot => snapshot.Symbol));

            var currentGrowthHoldings = currentWeights
                .Where(pair => pair.Value > 0m && growthUniverse.Contains(pair.Key))
                .Select(pair => pair.Key)
                .ToList();
            var currentDefensiveHoldings = currentWeights
                .Where(pair => pair.Value > 0m && defensiveUniverse.Contains(pair.Key))
                .Select(pair => pair.Key)
                .ToList();

            var currentGrowthWeight = currentGrowthHoldings.Sum(symbol => currentWeights[symbol]);
            var currentDefensiveWeight = currentDefensiveHoldings.Sum(symbol => currentWeights[symbol]);
            var trimOnly = previousRegime > activeRegime && growthSelection.ForcedExitSymbols.Count == 0;

            var currentEligibleGrowth = currentGrowthHoldings
                .Where(symbol =>
                    !growthSelection.ForcedExitSymbols.Contains(symbol) &&
                    growthSelection.CandidatesBySymbol.ContainsKey(symbol))
                .OrderByDescending(symbol => growthSelection.CandidatesBySymbol[symbol].AdjustedScore)
                .ThenBy(symbol => symbol.Value, System.StringComparer.Ordinal)
                .ToList();

            var finalGrowth = currentEligibleGrowth
                .Take(growthSelection.TargetHoldingCount)
                .ToList();

            var newEntriesUsed = 0;
            var optimizationReplacementsUsed = 0;

            if (!trimOnly && currentGrowthWeight <= sleeveTargets.GrowthTarget && finalGrowth.Count > 0)
            {
                while (optimizationReplacementsUsed < StrategyConfig.MaxOptimizationReplacementsPerWeek &&
                       newEntriesUsed < StrategyConfig.MaxRankingDrivenEntriesPerWeek)
                {
                    var worstHeld = finalGrowth
                        .OrderBy(symbol => growthSelection.CandidatesBySymbol[symbol].AdjustedScore)
                        .ThenBy(symbol => symbol.Value, System.StringComparer.Ordinal)
                        .FirstOrDefault();

                    if (worstHeld == null)
                    {
                        break;
                    }

                    var bestNonHeld = growthSelection.RankedCandidates
                        .FirstOrDefault(candidate => !finalGrowth.Contains(candidate.Symbol));

                    if (bestNonHeld == null)
                    {
                        break;
                    }

                    var worstHeldScore = growthSelection.CandidatesBySymbol[worstHeld].AdjustedScore;
                    if (bestNonHeld.AdjustedScore < worstHeldScore + StrategyConfig.ReplacementScoreGap)
                    {
                        break;
                    }

                    finalGrowth.Remove(worstHeld);
                    finalGrowth.Add(bestNonHeld.Symbol);
                    optimizationReplacementsUsed++;
                    newEntriesUsed++;
                }
            }

            foreach (var candidate in growthSelection.RankedCandidates)
            {
                if (finalGrowth.Count >= growthSelection.TargetHoldingCount)
                {
                    break;
                }

                if (finalGrowth.Contains(candidate.Symbol))
                {
                    continue;
                }

                if (newEntriesUsed >= StrategyConfig.MaxRankingDrivenEntriesPerWeek)
                {
                    break;
                }

                finalGrowth.Add(candidate.Symbol);
                newEntriesUsed++;
            }

            finalGrowth = finalGrowth
                .Distinct()
                .OrderBy(symbol => symbol.Value, System.StringComparer.Ordinal)
                .ToList();

            var finalDefensive = defensiveSelection.RankedCandidates
                .Take(defensiveSelection.TargetHoldingCount)
                .Select(candidate => candidate.Symbol)
                .Distinct()
                .OrderBy(symbol => symbol.Value, System.StringComparer.Ordinal)
                .ToList();

            var targetWeights = new Dictionary<Symbol, decimal>();
            var growthWeightPerSymbol = finalGrowth.Count > 0
                ? System.Math.Min(StrategyConfig.SingleGrowthWeightCap, sleeveTargets.GrowthTarget / finalGrowth.Count)
                : 0m;
            foreach (var symbol in finalGrowth)
            {
                targetWeights[symbol] = growthWeightPerSymbol;
            }

            var defensiveWeightPerSymbol = finalDefensive.Count > 0
                ? sleeveTargets.DefensiveTarget / finalDefensive.Count
                : 0m;
            foreach (var symbol in finalDefensive)
            {
                targetWeights[symbol] = defensiveWeightPerSymbol;
            }

            foreach (var symbol in currentGrowthHoldings.Concat(currentDefensiveHoldings).Distinct())
            {
                if (!targetWeights.ContainsKey(symbol))
                {
                    targetWeights[symbol] = 0m;
                }
            }

            var releasedReserve = 0m;
            var releaseRate = StrategyConfig.ReserveReleaseRateByRegime[activeRegime];
            if (undeployedCapitalReserve > 0m &&
                releaseRate > 0m &&
                (currentGrowthWeight < sleeveTargets.GrowthTarget || currentDefensiveWeight < sleeveTargets.DefensiveTarget))
            {
                releasedReserve = undeployedCapitalReserve * releaseRate;
            }

            return new PortfolioPlan(
                previousRegime,
                activeRegime,
                sleeveTargets,
                currentGrowthWeight,
                currentDefensiveWeight,
                trimOnly,
                growthSelection.ForcedExitSymbols,
                finalGrowth,
                finalDefensive,
                targetWeights,
                releasedReserve,
                optimizationReplacementsUsed,
                newEntriesUsed);
        }
    }

    public sealed class PortfolioPlan
    {
        public PortfolioPlan(
            RiskRegime previousRegime,
            RiskRegime activeRegime,
            SleeveTargets sleeveTargets,
            decimal currentGrowthWeight,
            decimal currentDefensiveWeight,
            bool trimOnly,
            IReadOnlyCollection<Symbol> forcedExitSymbols,
            IReadOnlyList<Symbol> selectedGrowthSymbols,
            IReadOnlyList<Symbol> selectedDefensiveSymbols,
            IReadOnlyDictionary<Symbol, decimal> targetWeights,
            decimal releasedReserve,
            int optimizationReplacementsUsed,
            int newEntriesUsed)
        {
            PreviousRegime = previousRegime;
            ActiveRegime = activeRegime;
            SleeveTargets = sleeveTargets;
            CurrentGrowthWeight = currentGrowthWeight;
            CurrentDefensiveWeight = currentDefensiveWeight;
            TrimOnly = trimOnly;
            ForcedExitSymbols = forcedExitSymbols;
            SelectedGrowthSymbols = selectedGrowthSymbols;
            SelectedDefensiveSymbols = selectedDefensiveSymbols;
            TargetWeights = targetWeights;
            ReleasedReserve = releasedReserve;
            OptimizationReplacementsUsed = optimizationReplacementsUsed;
            NewEntriesUsed = newEntriesUsed;
        }

        public RiskRegime PreviousRegime { get; }
        public RiskRegime ActiveRegime { get; }
        public SleeveTargets SleeveTargets { get; }
        public decimal CurrentGrowthWeight { get; }
        public decimal CurrentDefensiveWeight { get; }
        public bool TrimOnly { get; }
        public IReadOnlyCollection<Symbol> ForcedExitSymbols { get; }
        public IReadOnlyList<Symbol> SelectedGrowthSymbols { get; }
        public IReadOnlyList<Symbol> SelectedDefensiveSymbols { get; }
        public IReadOnlyDictionary<Symbol, decimal> TargetWeights { get; }
        public decimal ReleasedReserve { get; }
        public int OptimizationReplacementsUsed { get; }
        public int NewEntriesUsed { get; }
    }
}
