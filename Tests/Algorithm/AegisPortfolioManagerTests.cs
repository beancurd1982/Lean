using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AegisPortfolioManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            QuantConnect.Algorithm.CSharp.StrategyConfig.ResetRuntimeParameters();
        }

        [TearDown]
        public void TearDown()
        {
            QuantConnect.Algorithm.CSharp.StrategyConfig.ResetRuntimeParameters();
        }

        [Test]
        public void PreservesCurrentWeightsWhenSleevesRemainWithinToleranceBands()
        {
            var growthSymbols = new[]
            {
                CreateSymbol("AAPL"),
                CreateSymbol("AMZN"),
                CreateSymbol("GOOGL"),
                CreateSymbol("META"),
                CreateSymbol("MSFT"),
                CreateSymbol("NVDA")
            };
            var defensiveSymbols = new[]
            {
                CreateSymbol("SCHD"),
                CreateSymbol("VIG")
            };

            var currentWeights = growthSymbols.ToDictionary(symbol => symbol, _ => 0.105m);
            currentWeights[defensiveSymbols[0]] = 0.11m;
            currentWeights[defensiveSymbols[1]] = 0.11m;

            var growthSelection = CreateGrowthSelection(growthSymbols, growthSymbols, System.Array.Empty<Symbol>(), targetHoldingCount: 6);
            var defensiveSelection = CreateDefensiveSelection(defensiveSymbols, targetHoldingCount: 2);

            var plan = new QuantConnect.Algorithm.CSharp.PortfolioManager().BuildPlan(
                QuantConnect.Algorithm.CSharp.RiskRegime.Favorable,
                QuantConnect.Algorithm.CSharp.RiskRegime.Favorable,
                growthSelection,
                defensiveSelection,
                currentWeights,
                undeployedCapitalReserve: 0m,
                totalPortfolioValue: 1m);

            Assert.That(plan.TargetWeights.OrderBy(pair => pair.Key.Value), Is.EqualTo(currentWeights.OrderBy(pair => pair.Key.Value)));
            Assert.That(plan.ReleasedReserveWeight, Is.EqualTo(0m));
        }

        [Test]
        public void RebalancesToSleeveTargetsWhenGrowthSleeveIsOutsideToleranceBand()
        {
            var growthSymbols = new[]
            {
                CreateSymbol("AAPL"),
                CreateSymbol("AMZN"),
                CreateSymbol("GOOGL"),
                CreateSymbol("META"),
                CreateSymbol("MSFT"),
                CreateSymbol("NVDA")
            };
            var defensiveSymbols = new[]
            {
                CreateSymbol("SCHD"),
                CreateSymbol("VIG")
            };

            var currentWeights = growthSymbols.ToDictionary(symbol => symbol, _ => 0.0833333333m);
            currentWeights[defensiveSymbols[0]] = 0.10m;
            currentWeights[defensiveSymbols[1]] = 0.10m;

            var growthSelection = CreateGrowthSelection(growthSymbols, growthSymbols, System.Array.Empty<Symbol>(), targetHoldingCount: 6);
            var defensiveSelection = CreateDefensiveSelection(defensiveSymbols, targetHoldingCount: 2);

            var plan = new QuantConnect.Algorithm.CSharp.PortfolioManager().BuildPlan(
                QuantConnect.Algorithm.CSharp.RiskRegime.Favorable,
                QuantConnect.Algorithm.CSharp.RiskRegime.Favorable,
                growthSelection,
                defensiveSelection,
                currentWeights,
                undeployedCapitalReserve: 0m,
                totalPortfolioValue: 1m);

            var targetGrowthWeight = plan.TargetWeights
                .Where(pair => growthSymbols.Contains(pair.Key))
                .Sum(pair => pair.Value);
            var targetDefensiveWeight = plan.TargetWeights
                .Where(pair => defensiveSymbols.Contains(pair.Key))
                .Sum(pair => pair.Value);

            Assert.That(targetGrowthWeight, Is.EqualTo(0.65m).Within(0.000001m));
            Assert.That(targetDefensiveWeight, Is.EqualTo(0.20m).Within(0.000001m));
            Assert.That(plan.TargetWeights[growthSymbols[0]], Is.EqualTo(0.1083333333m).Within(0.000001m));
        }

        [Test]
        public void LimitsBuysToReleasedReserveAndPrioritizesExistingGrowthHoldings()
        {
            var currentGrowthSymbols = new[]
            {
                CreateSymbol("AAPL"),
                CreateSymbol("AMZN"),
                CreateSymbol("GOOGL"),
                CreateSymbol("MSFT")
            };
            var newGrowthSymbols = new[]
            {
                CreateSymbol("META"),
                CreateSymbol("NVDA")
            };
            var defensiveSymbols = new[]
            {
                CreateSymbol("SCHD"),
                CreateSymbol("VIG")
            };

            var currentWeights = currentGrowthSymbols.ToDictionary(symbol => symbol, _ => 0.075m);
            currentWeights[defensiveSymbols[0]] = 0.10m;
            currentWeights[defensiveSymbols[1]] = 0.10m;

            var allGrowthSymbols = currentGrowthSymbols.Concat(newGrowthSymbols).ToArray();
            var growthSelection = CreateGrowthSelection(allGrowthSymbols, allGrowthSymbols, System.Array.Empty<Symbol>(), targetHoldingCount: 6);
            var defensiveSelection = CreateDefensiveSelection(defensiveSymbols, targetHoldingCount: 2);

            var plan = new QuantConnect.Algorithm.CSharp.PortfolioManager().BuildPlan(
                QuantConnect.Algorithm.CSharp.RiskRegime.Favorable,
                QuantConnect.Algorithm.CSharp.RiskRegime.Favorable,
                growthSelection,
                defensiveSelection,
                currentWeights,
                undeployedCapitalReserve: 0.15m,
                totalPortfolioValue: 1m);

            var currentInvestedWeight = currentWeights.Values.Sum();
            var targetInvestedWeight = plan.TargetWeights.Values.Sum();

            Assert.That(plan.ReleasedReserve, Is.EqualTo(0.0495m).Within(0.000001m));
            Assert.That(plan.ReleasedReserveWeight, Is.EqualTo(0.0495m).Within(0.000001m));
            Assert.That(targetInvestedWeight, Is.EqualTo(currentInvestedWeight + plan.ReleasedReserveWeight).Within(0.000001m));

            foreach (var symbol in currentGrowthSymbols)
            {
                Assert.That(plan.TargetWeights[symbol], Is.GreaterThan(currentWeights[symbol]));
                Assert.That(plan.TargetWeights[symbol], Is.LessThan(0.1083333334m));
            }

            foreach (var symbol in newGrowthSymbols)
            {
                Assert.That(plan.TargetWeights[symbol], Is.EqualTo(0m).Within(0.000001m));
            }

            Assert.That(plan.TargetWeights[defensiveSymbols[0]], Is.EqualTo(0.10m).Within(0.000001m));
            Assert.That(plan.TargetWeights[defensiveSymbols[1]], Is.EqualTo(0.10m).Within(0.000001m));
        }

        [Test]
        public void NarrowerToleranceBandScaleTriggersRebalanceInsideDefaultBand()
        {
            QuantConnect.Algorithm.CSharp.StrategyConfig.ConfigureRuntimeParameters(
                QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultFavorableBreadthThreshold,
                QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultWeakStressThreshold,
                QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultUpgradeConfirmationWeeks,
                QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultGrowthAtrEligibilityLimit,
                QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultReplacementScoreGap,
                QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultHoldStabilityBonus,
                rebalanceToleranceBandScale: 0.5m);

            var growthSymbols = new[]
            {
                CreateSymbol("AAPL"),
                CreateSymbol("AMZN"),
                CreateSymbol("GOOGL"),
                CreateSymbol("META"),
                CreateSymbol("MSFT"),
                CreateSymbol("NVDA")
            };
            var defensiveSymbols = new[]
            {
                CreateSymbol("SCHD"),
                CreateSymbol("VIG")
            };

            var currentWeights = growthSymbols.ToDictionary(symbol => symbol, _ => 0.1016666667m);
            currentWeights[defensiveSymbols[0]] = 0.10m;
            currentWeights[defensiveSymbols[1]] = 0.10m;

            var growthSelection = CreateGrowthSelection(growthSymbols, growthSymbols, System.Array.Empty<Symbol>(), targetHoldingCount: 6);
            var defensiveSelection = CreateDefensiveSelection(defensiveSymbols, targetHoldingCount: 2);

            var plan = new QuantConnect.Algorithm.CSharp.PortfolioManager().BuildPlan(
                QuantConnect.Algorithm.CSharp.RiskRegime.Favorable,
                QuantConnect.Algorithm.CSharp.RiskRegime.Favorable,
                growthSelection,
                defensiveSelection,
                currentWeights,
                undeployedCapitalReserve: 0m,
                totalPortfolioValue: 1m);

            Assert.That(plan.SleevesWithinToleranceBands, Is.False);
            Assert.That(plan.HasRebalanceTrigger, Is.True);
            Assert.That(plan.TargetWeights[growthSymbols[0]], Is.EqualTo(0.1083333333m).Within(0.000001m));
        }

        private static QuantConnect.Algorithm.CSharp.GrowthSelection CreateGrowthSelection(
            IReadOnlyCollection<Symbol> rankedSymbols,
            IReadOnlyCollection<Symbol> allSymbols,
            IReadOnlyCollection<Symbol> forcedExitSymbols,
            int targetHoldingCount)
        {
            var snapshots = allSymbols
                .Select((symbol, index) => CreateSnapshot(symbol, symbol.Value, isGrowth: true, isDefensive: false, scoreSeed: index))
                .ToList();
            var rankedCandidates = rankedSymbols
                .Select((symbol, index) =>
                {
                    var snapshot = snapshots.Single(item => item.Symbol == symbol);
                    return new QuantConnect.Algorithm.CSharp.GrowthCandidate(
                        snapshot,
                        isCurrentHolding: false,
                        trendScore: 30m,
                        relativeStrengthScore: 30m - index,
                        stabilityScore: 20m,
                        penalty: 0m,
                        finalScore: 80m - index,
                        adjustedScore: 80m - index);
                })
                .ToList();

            return new QuantConnect.Algorithm.CSharp.GrowthSelection(
                snapshots,
                rankedCandidates,
                forcedExitSymbols,
                targetHoldingCount);
        }

        private static QuantConnect.Algorithm.CSharp.DefensiveSelection CreateDefensiveSelection(
            IReadOnlyCollection<Symbol> rankedSymbols,
            int targetHoldingCount)
        {
            var snapshots = rankedSymbols
                .Select((symbol, index) => CreateSnapshot(symbol, symbol.Value, isGrowth: false, isDefensive: true, scoreSeed: index))
                .ToList();
            var rankedCandidates = snapshots
                .Select((snapshot, index) => new QuantConnect.Algorithm.CSharp.DefensiveCandidate(snapshot, 1m - (index * 0.1m)))
                .ToList();

            return new QuantConnect.Algorithm.CSharp.DefensiveSelection(
                snapshots,
                rankedCandidates,
                targetHoldingCount);
        }

        private static QuantConnect.Algorithm.CSharp.AssetSnapshot CreateSnapshot(
            Symbol symbol,
            string ticker,
            bool isGrowth,
            bool isDefensive,
            int scoreSeed)
        {
            var close = 100m + scoreSeed;
            return new QuantConnect.Algorithm.CSharp.AssetSnapshot(
                symbol,
                ticker,
                isGrowth,
                isDefensive,
                isSgov: ticker == "SGOV",
                isDataReady: true,
                close: close,
                sma50: close - 2m,
                sma200: close - 5m,
                atr20: 2m,
                return21: 0.05m,
                return63: 0.10m,
                return126: 0.20m,
                volatility63: 0.10m,
                drawdown63: 0.08m);
        }

        private static Symbol CreateSymbol(string ticker)
        {
            return Symbol.Create(ticker, SecurityType.Equity, Market.USA);
        }
    }
}
