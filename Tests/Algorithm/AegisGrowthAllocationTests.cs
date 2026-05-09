using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Interfaces;
using QuantConnect.Packets;
using QuantConnect.Scheduling;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class AegisGrowthAllocationTests
    {
        [Test]
        public void RegistersExchangeAwareWeeklyReviewSchedule()
        {
            var algorithm = new QuantConnect.Algorithm.CSharp.AegisGrowthAllocation();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));

            var scheduleCapture = new CapturingRealTimeHandler();
            algorithm.Schedule.SetEventSchedule(scheduleCapture);

            algorithm.Initialize();

            var scheduledEvent = scheduleCapture.AddedEvents.Single();

            Assert.That(scheduledEvent.Name, Does.Contain("WeekStart"));
            Assert.That(scheduledEvent.Name, Does.Contain("MarketOpen"));
            Assert.That(scheduledEvent.Name, Does.Not.Contain("Every"));
            Assert.That(scheduledEvent.Name, Does.Not.Contain("10:00"));
        }

        [Test]
        public void UsesDefaultBacktestStartDateWhenBacktestDateParametersAreAbsent()
        {
            var algorithm = CreateAlgorithm();

            Assert.That(algorithm.StartDate, Is.EqualTo(new DateTime(2018, 1, 1)));
        }

        [Test]
        public void UsesConfiguredBacktestDateParametersWhenProvided()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["backtest-start"] = "2007-10-01",
                ["backtest-end"] = "2010-12-31"
            });

            Assert.That(algorithm.StartDate, Is.EqualTo(new DateTime(2007, 10, 1)));
            Assert.That(algorithm.EndDate, Is.EqualTo(new DateTime(2010, 12, 31).AddDays(1).AddTicks(-1)));
        }

        [Test]
        public void IgnoresInvalidBacktestDateParameters()
        {
            var baseline = CreateAlgorithm();
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["backtest-start"] = "not-a-date",
                ["backtest-end"] = "still-not-a-date"
            });

            Assert.That(algorithm.StartDate, Is.EqualTo(baseline.StartDate));
            Assert.That(algorithm.EndDate, Is.EqualTo(baseline.EndDate));
        }

        [Test]
        public void IgnoresBacktestEndDateEarlierThanBacktestStartDate()
        {
            var baseline = CreateAlgorithm(new Dictionary<string, string>
            {
                ["backtest-start"] = "2007-10-01"
            });
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["backtest-start"] = "2007-10-01",
                ["backtest-end"] = "2007-09-30"
            });

            Assert.That(algorithm.StartDate, Is.EqualTo(baseline.StartDate));
            Assert.That(algorithm.EndDate, Is.EqualTo(baseline.EndDate));
        }

        [Test]
        public void DisablesCrisisDiagnosticsByDefault()
        {
            var algorithm = CreateAlgorithm();

            Assert.That(IsCrisisDiagnosticsEnabled(algorithm), Is.False);
        }

        [Test]
        public void EnablesCrisisDiagnosticsWhenParameterIsTrue()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["crisis-diagnostics"] = "true"
            });

            Assert.That(IsCrisisDiagnosticsEnabled(algorithm), Is.True);
        }

        [Test]
        public void IgnoresInvalidCrisisDiagnosticsParameter()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["crisis-diagnostics"] = "not-a-bool"
            });

            Assert.That(IsCrisisDiagnosticsEnabled(algorithm), Is.False);
        }

        [Test]
        public void DisablesWeakStressOverlayByDefault()
        {
            var algorithm = CreateAlgorithm();

            Assert.That(IsWeakStressOverlayEnabled(algorithm), Is.False);
        }

        [Test]
        public void EnablesWeakStressOverlayWhenParameterIsTrue()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["weak-stress-overlay-enabled"] = "true"
            });

            Assert.That(IsWeakStressOverlayEnabled(algorithm), Is.True);
        }

        [Test]
        public void IgnoresInvalidWeakStressOverlayParameter()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["weak-stress-overlay-enabled"] = "not-a-bool"
            });

            Assert.That(IsWeakStressOverlayEnabled(algorithm), Is.False);
        }

        [Test]
        public void PortfolioManagerKeepsDefaultWeakSleevesWithoutOverlay()
        {
            var growthSymbol = QuantConnect.Symbol.Create("AAPL", QuantConnect.SecurityType.Equity, QuantConnect.Market.USA);
            var defensiveSymbol = QuantConnect.Symbol.Create("SGOV", QuantConnect.SecurityType.Equity, QuantConnect.Market.USA);
            var plan = BuildWeakPlan(growthSymbol, defensiveSymbol);

            Assert.That(plan.SelectedGrowthSymbols, Is.EquivalentTo(new[] { growthSymbol }));
            Assert.That(plan.TargetWeights[growthSymbol], Is.EqualTo(0.10m));
            Assert.That(plan.TargetWeights[defensiveSymbol], Is.EqualTo(0.40m));
            Assert.That(1m - plan.TargetWeights.Values.Sum(), Is.EqualTo(0.50m));
        }

        [Test]
        public void PortfolioManagerAppliesWeakStressOverlaySleeves()
        {
            var growthSymbol = QuantConnect.Symbol.Create("AAPL", QuantConnect.SecurityType.Equity, QuantConnect.Market.USA);
            var defensiveSymbol = QuantConnect.Symbol.Create("SGOV", QuantConnect.SecurityType.Equity, QuantConnect.Market.USA);
            var plan = BuildWeakPlan(
                growthSymbol,
                defensiveSymbol,
                QuantConnect.Algorithm.CSharp.StrategyConfig.WeakStressOverlaySleeveTargets);

            Assert.That(plan.SelectedGrowthSymbols, Is.Empty);
            Assert.That(plan.TargetWeights[growthSymbol], Is.EqualTo(0m));
            Assert.That(plan.TargetWeights[defensiveSymbol], Is.EqualTo(0.20m));
            Assert.That(1m - plan.TargetWeights.Values.Sum(), Is.EqualTo(0.80m));
        }

        [Test]
        public void DisablesPreWeakGuardByDefault()
        {
            var algorithm = CreateAlgorithm();

            Assert.That(IsPreWeakGuardEnabled(algorithm), Is.False);
        }

        [Test]
        public void EnablesPreWeakGuardWhenParameterIsTrue()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "true"
            });

            Assert.That(IsPreWeakGuardEnabled(algorithm), Is.True);
        }

        [Test]
        public void IgnoresInvalidPreWeakGuardParameter()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "not-a-bool"
            });

            Assert.That(IsPreWeakGuardEnabled(algorithm), Is.False);
        }

        [Test]
        public void UsesConfiguredPreWeakGuardDrawdownThreshold()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "true",
                ["pre-weak-guard-drawdown-threshold"] = "0.04"
            });

            Assert.That(GetPreWeakGuardDrawdownThreshold(algorithm), Is.EqualTo(0.04m));
        }

        [Test]
        public void PreWeakGuardRequiresEnabledParameter()
        {
            var algorithm = CreateAlgorithm();
            SetPrivateField(algorithm, "_preWeakGuardEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplyPreWeakGuard(algorithm, CreateNeutralDeterioratingSnapshot(), 94000m), Is.False);
        }

        [Test]
        public void PreWeakGuardDoesNotApplyInWeakRegime()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_preWeakGuardEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplyPreWeakGuard(algorithm, CreateWeakSnapshot(), 94000m), Is.False);
        }

        [Test]
        public void PreWeakGuardRequiresDrawdownThreshold()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_preWeakGuardEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplyPreWeakGuard(algorithm, CreateNeutralDeterioratingSnapshot(), 96000m), Is.False);
        }

        [Test]
        public void PreWeakGuardRequiresDeterioratingSignal()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_preWeakGuardEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplyPreWeakGuard(algorithm, CreateFavorableSnapshot(), 94000m), Is.False);
        }

        [Test]
        public void PreWeakGuardAppliesBeforeWeakWhenDrawdownAndSignalsDeteriorate()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_preWeakGuardEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplyPreWeakGuard(algorithm, CreateNeutralDeterioratingSnapshot(), 94000m), Is.True);
        }

        private static QuantConnect.Algorithm.CSharp.AegisGrowthAllocation CreateAlgorithm(
            IReadOnlyDictionary<string, string> parameters = null)
        {
            var algorithm = new QuantConnect.Algorithm.CSharp.AegisGrowthAllocation();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            if (parameters != null)
            {
                algorithm.SetParameters(parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }

            algorithm.Initialize();
            return algorithm;
        }

        private static bool IsCrisisDiagnosticsEnabled(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField("_crisisDiagnosticsEnabled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (bool)field.GetValue(algorithm);
        }

        private static bool IsWeakStressOverlayEnabled(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField("_weakStressOverlayEnabled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (bool)field.GetValue(algorithm);
        }

        private static bool IsPreWeakGuardEnabled(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField("_preWeakGuardEnabled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (bool)field.GetValue(algorithm);
        }

        private static decimal GetPreWeakGuardDrawdownThreshold(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField("_preWeakGuardDrawdownThreshold", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (decimal)field.GetValue(algorithm);
        }

        private static void SetPrivateField<T>(
            QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm,
            string name,
            T value)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            field.SetValue(algorithm, value);
        }

        private static bool ShouldApplyPreWeakGuard(
            QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm,
            QuantConnect.Algorithm.CSharp.RegimeSnapshot regimeSnapshot,
            decimal totalPortfolioValue)
        {
            var method = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetMethod("ShouldApplyPreWeakGuard", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(algorithm, new object[] { regimeSnapshot, totalPortfolioValue });
        }

        private static QuantConnect.Algorithm.CSharp.RegimeSnapshot CreateNeutralDeterioratingSnapshot()
        {
            return new QuantConnect.Algorithm.CSharp.RegimeSnapshot(
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                QuantConnect.Algorithm.CSharp.SignalState.Neutral,
                QuantConnect.Algorithm.CSharp.SignalState.Weak,
                QuantConnect.Algorithm.CSharp.SignalState.Neutral,
                severeStress: false,
                upgradeConfirmationCount: 0);
        }

        private static QuantConnect.Algorithm.CSharp.RegimeSnapshot CreateWeakSnapshot()
        {
            return new QuantConnect.Algorithm.CSharp.RegimeSnapshot(
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                QuantConnect.Algorithm.CSharp.RiskRegime.Weak,
                QuantConnect.Algorithm.CSharp.RiskRegime.Weak,
                QuantConnect.Algorithm.CSharp.SignalState.Weak,
                QuantConnect.Algorithm.CSharp.SignalState.Weak,
                QuantConnect.Algorithm.CSharp.SignalState.Weak,
                severeStress: true,
                upgradeConfirmationCount: 0);
        }

        private static QuantConnect.Algorithm.CSharp.RegimeSnapshot CreateFavorableSnapshot()
        {
            return new QuantConnect.Algorithm.CSharp.RegimeSnapshot(
                QuantConnect.Algorithm.CSharp.RiskRegime.Favorable,
                QuantConnect.Algorithm.CSharp.RiskRegime.Favorable,
                QuantConnect.Algorithm.CSharp.RiskRegime.Favorable,
                QuantConnect.Algorithm.CSharp.SignalState.Favorable,
                QuantConnect.Algorithm.CSharp.SignalState.Favorable,
                QuantConnect.Algorithm.CSharp.SignalState.Favorable,
                severeStress: false,
                upgradeConfirmationCount: 0);
        }

        private static QuantConnect.Algorithm.CSharp.PortfolioPlan BuildWeakPlan(
            QuantConnect.Symbol growthSymbol,
            QuantConnect.Symbol defensiveSymbol,
            QuantConnect.Algorithm.CSharp.SleeveTargets sleeveTargetsOverride = null)
        {
            var growthSnapshot = CreateSnapshot(growthSymbol, "AAPL", isGrowth: true, isDefensive: false, isSgov: false);
            var defensiveSnapshot = CreateSnapshot(defensiveSymbol, "SGOV", isGrowth: false, isDefensive: true, isSgov: true);
            var growthSelection = new QuantConnect.Algorithm.CSharp.GrowthSelection(
                new[] { growthSnapshot },
                new[]
                {
                    new QuantConnect.Algorithm.CSharp.GrowthCandidate(
                        growthSnapshot,
                        isCurrentHolding: true,
                        trendScore: 10m,
                        relativeStrengthScore: 10m,
                        stabilityScore: 10m,
                        penalty: 0m,
                        finalScore: 30m,
                        adjustedScore: 32m)
                },
                Array.Empty<QuantConnect.Symbol>(),
                targetHoldingCount: 1);
            var defensiveSelection = new QuantConnect.Algorithm.CSharp.DefensiveSelection(
                new[] { defensiveSnapshot },
                new[]
                {
                    new QuantConnect.Algorithm.CSharp.DefensiveCandidate(defensiveSnapshot, score: 1m)
                },
                targetHoldingCount: 1);
            var currentWeights = new Dictionary<QuantConnect.Symbol, decimal>
            {
                [growthSymbol] = 0.45m,
                [defensiveSymbol] = 0.30m
            };

            return new QuantConnect.Algorithm.CSharp.PortfolioManager().BuildPlan(
                QuantConnect.Algorithm.CSharp.RiskRegime.Weak,
                QuantConnect.Algorithm.CSharp.RiskRegime.Weak,
                growthSelection,
                defensiveSelection,
                currentWeights,
                undeployedCapitalReserve: 0m,
                totalPortfolioValue: 100000m,
                sleeveTargetsOverride: sleeveTargetsOverride);
        }

        private static QuantConnect.Algorithm.CSharp.AssetSnapshot CreateSnapshot(
            QuantConnect.Symbol symbol,
            string ticker,
            bool isGrowth,
            bool isDefensive,
            bool isSgov)
        {
            return new QuantConnect.Algorithm.CSharp.AssetSnapshot(
                symbol,
                ticker,
                isGrowth,
                isDefensive,
                isSgov,
                isDataReady: true,
                close: 100m,
                sma50: 105m,
                sma200: 95m,
                atr20: 2m,
                return21: 0.03m,
                return63: 0.05m,
                return126: 0.08m,
                volatility63: 0.10m,
                drawdown63: -0.02m);
        }

        private sealed class CapturingRealTimeHandler : IRealTimeHandler
        {
            public List<ScheduledEvent> AddedEvents { get; } = new List<ScheduledEvent>();

            public bool IsActive => false;

            public void Add(ScheduledEvent scheduledEvent)
            {
                AddedEvents.Add(scheduledEvent);
            }

            public void Remove(ScheduledEvent scheduledEvent)
            {
            }

            public void Setup(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IApi api, IIsolatorLimitResultProvider isolatorLimitProvider)
            {
            }

            public void SetTime(DateTime time)
            {
            }

            public void ScanPastEvents(DateTime time)
            {
            }

            public void Exit()
            {
            }

            public void OnSecuritiesChanged(SecurityChanges changes)
            {
            }
        }
    }
}
