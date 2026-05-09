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
            decimal undeployedCapitalReserve,
            decimal totalPortfolioValue,
            SleeveTargets sleeveTargetsOverride = null)
        {
            var sleeveTargets = sleeveTargetsOverride ?? StrategyConfig.GetSleeveTargets(activeRegime);
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
            var currentCashWeight = System.Math.Max(0m, 1m - currentGrowthWeight - currentDefensiveWeight);
            var trimOnly = previousRegime > activeRegime && growthSelection.ForcedExitSymbols.Count == 0;

            var currentEligibleGrowth = currentGrowthHoldings
                .Where(symbol =>
                    !growthSelection.ForcedExitSymbols.Contains(symbol) &&
                    growthSelection.CandidatesBySymbol.ContainsKey(symbol))
                .OrderByDescending(symbol => growthSelection.CandidatesBySymbol[symbol].AdjustedScore)
                .ThenBy(symbol => symbol.Value, System.StringComparer.Ordinal)
                .ToList();

            var finalGrowth = sleeveTargets.GrowthTarget > 0m
                ? currentEligibleGrowth
                    .Take(growthSelection.TargetHoldingCount)
                    .ToList()
                : new List<Symbol>();

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
                if (sleeveTargets.GrowthTarget <= 0m)
                {
                    break;
                }

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

            var exactTargetWeights = new Dictionary<Symbol, decimal>();
            var growthWeightPerSymbol = finalGrowth.Count > 0
                ? System.Math.Min(StrategyConfig.SingleGrowthWeightCap, sleeveTargets.GrowthTarget / finalGrowth.Count)
                : 0m;
            foreach (var symbol in finalGrowth)
            {
                exactTargetWeights[symbol] = growthWeightPerSymbol;
            }

            var defensiveWeightPerSymbol = finalDefensive.Count > 0
                ? sleeveTargets.DefensiveTarget / finalDefensive.Count
                : 0m;
            foreach (var symbol in finalDefensive)
            {
                exactTargetWeights[symbol] = defensiveWeightPerSymbol;
            }

            foreach (var symbol in currentGrowthHoldings.Concat(currentDefensiveHoldings).Distinct())
            {
                if (!exactTargetWeights.ContainsKey(symbol))
                {
                    exactTargetWeights[symbol] = 0m;
                }
            }

            var requestedReserveWeight = 0m;
            var releaseRate = StrategyConfig.ReserveReleaseRateByRegime[activeRegime];
            var reserveWeight = NormalizeReserveWeight(undeployedCapitalReserve, totalPortfolioValue);
            if (undeployedCapitalReserve > 0m &&
                releaseRate > 0m &&
                (currentGrowthWeight < sleeveTargets.GrowthTarget || currentDefensiveWeight < sleeveTargets.DefensiveTarget))
            {
                requestedReserveWeight = System.Math.Min(currentCashWeight, reserveWeight * releaseRate);
            }

            var sleevesWithinToleranceBands =
                sleeveTargets.IsGrowthWithinBand(currentGrowthWeight) &&
                sleeveTargets.IsDefensiveWithinBand(currentDefensiveWeight) &&
                sleeveTargets.IsCashWithinBand(currentCashWeight);
            var hasSelectionChange = HasSelectionChange(
                currentGrowthHoldings,
                currentDefensiveHoldings,
                finalGrowth,
                finalDefensive);
            var hasRebalanceTrigger =
                !sleevesWithinToleranceBands ||
                growthSelection.ForcedExitSymbols.Count > 0 ||
                hasSelectionChange ||
                requestedReserveWeight > 0m;

            var targetWeights = !hasRebalanceTrigger
                ? new Dictionary<Symbol, decimal>(currentWeights)
                : requestedReserveWeight > 0m
                    ? BuildReserveAwareTargetWeights(
                        currentWeights,
                        exactTargetWeights,
                        finalGrowth,
                        finalDefensive,
                        requestedReserveWeight)
                    : exactTargetWeights;
            var releasedReserveWeight = System.Math.Max(0m, targetWeights.Values.Sum() - currentWeights.Values.Sum());
            var releasedReserve = DenormalizeReserveAmount(releasedReserveWeight, undeployedCapitalReserve, totalPortfolioValue);
            hasRebalanceTrigger =
                !sleevesWithinToleranceBands ||
                growthSelection.ForcedExitSymbols.Count > 0 ||
                hasSelectionChange ||
                releasedReserveWeight > 0m;
            if (!hasRebalanceTrigger)
            {
                targetWeights = new Dictionary<Symbol, decimal>(currentWeights);
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
                releasedReserveWeight,
                sleevesWithinToleranceBands,
                hasSelectionChange,
                hasRebalanceTrigger,
                optimizationReplacementsUsed,
                newEntriesUsed);
        }

        private static bool HasSelectionChange(
            IReadOnlyCollection<Symbol> currentGrowthHoldings,
            IReadOnlyCollection<Symbol> currentDefensiveHoldings,
            IReadOnlyCollection<Symbol> finalGrowth,
            IReadOnlyCollection<Symbol> finalDefensive)
        {
            return !new HashSet<Symbol>(currentGrowthHoldings).SetEquals(finalGrowth) ||
                   !new HashSet<Symbol>(currentDefensiveHoldings).SetEquals(finalDefensive);
        }

        private static Dictionary<Symbol, decimal> BuildReserveAwareTargetWeights(
            IReadOnlyDictionary<Symbol, decimal> currentWeights,
            IReadOnlyDictionary<Symbol, decimal> exactTargetWeights,
            IReadOnlyCollection<Symbol> finalGrowth,
            IReadOnlyCollection<Symbol> finalDefensive,
            decimal releasedReserveWeight)
        {
            var targetWeights = new Dictionary<Symbol, decimal>();
            var availableBuyWeight = releasedReserveWeight;

            foreach (var pair in exactTargetWeights)
            {
                var currentWeight = currentWeights.TryGetValue(pair.Key, out var value)
                    ? value
                    : 0m;

                if (pair.Value < currentWeight)
                {
                    targetWeights[pair.Key] = pair.Value;
                    availableBuyWeight += currentWeight - pair.Value;
                    continue;
                }

                targetWeights[pair.Key] = currentWeight;
            }

            foreach (var pair in currentWeights)
            {
                if (!targetWeights.ContainsKey(pair.Key))
                {
                    targetWeights[pair.Key] = pair.Value;
                }
            }

            var existingGrowthSymbols = finalGrowth
                .Where(symbol => currentWeights.TryGetValue(symbol, out var weight) && weight > 0m)
                .ToList();
            var newGrowthSymbols = finalGrowth
                .Where(symbol => !currentWeights.TryGetValue(symbol, out var weight) || weight <= 0m)
                .ToList();
            var allocationPriority = existingGrowthSymbols
                .Concat(newGrowthSymbols)
                .Concat(finalDefensive)
                .ToList();

            foreach (var symbol in allocationPriority)
            {
                if (availableBuyWeight <= 0m || !exactTargetWeights.TryGetValue(symbol, out var desiredWeight))
                {
                    break;
                }

                var currentTargetWeight = targetWeights.TryGetValue(symbol, out var value)
                    ? value
                    : 0m;
                var requiredIncrease = desiredWeight - currentTargetWeight;
                if (requiredIncrease <= 0m)
                {
                    continue;
                }

                var increase = System.Math.Min(requiredIncrease, availableBuyWeight);
                targetWeights[symbol] = currentTargetWeight + increase;
                availableBuyWeight -= increase;
            }

            return targetWeights;
        }

        private static decimal NormalizeReserveWeight(decimal undeployedCapitalReserve, decimal totalPortfolioValue)
        {
            if (undeployedCapitalReserve <= 0m)
            {
                return 0m;
            }

            if (undeployedCapitalReserve <= 1m)
            {
                return undeployedCapitalReserve;
            }

            return totalPortfolioValue > 0m
                ? undeployedCapitalReserve / totalPortfolioValue
                : 0m;
        }

        private static decimal DenormalizeReserveAmount(
            decimal reserveWeight,
            decimal undeployedCapitalReserve,
            decimal totalPortfolioValue)
        {
            if (reserveWeight <= 0m || undeployedCapitalReserve <= 0m)
            {
                return 0m;
            }

            if (undeployedCapitalReserve <= 1m)
            {
                return System.Math.Min(reserveWeight, undeployedCapitalReserve);
            }

            return totalPortfolioValue > 0m
                ? System.Math.Min(undeployedCapitalReserve, reserveWeight * totalPortfolioValue)
                : 0m;
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
            decimal releasedReserveWeight,
            bool sleevesWithinToleranceBands,
            bool hasSelectionChange,
            bool hasRebalanceTrigger,
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
            ReleasedReserveWeight = releasedReserveWeight;
            SleevesWithinToleranceBands = sleevesWithinToleranceBands;
            HasSelectionChange = hasSelectionChange;
            HasRebalanceTrigger = hasRebalanceTrigger;
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
        public decimal ReleasedReserveWeight { get; }
        public bool SleevesWithinToleranceBands { get; }
        public bool HasSelectionChange { get; }
        public bool HasRebalanceTrigger { get; }
        public int OptimizationReplacementsUsed { get; }
        public int NewEntriesUsed { get; }
    }
}
