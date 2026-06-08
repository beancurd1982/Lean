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
        public void UsesDefaultRuntimeSleeveTargets()
        {
            var algorithm = CreateAlgorithm();

            var weakTargets = QuantConnect.Algorithm.CSharp.StrategyConfig.GetSleeveTargets(
                QuantConnect.Algorithm.CSharp.RiskRegime.Weak);
            var preWeakTargets = QuantConnect.Algorithm.CSharp.StrategyConfig.PreWeakGuardSleeveTargets;

            Assert.That(weakTargets.GrowthTarget, Is.EqualTo(0.10m));
            Assert.That(weakTargets.DefensiveTarget, Is.EqualTo(0.40m));
            Assert.That(weakTargets.CashTarget, Is.EqualTo(0.50m));
            Assert.That(preWeakTargets.GrowthTarget, Is.EqualTo(0.12m));
            Assert.That(preWeakTargets.DefensiveTarget, Is.EqualTo(0.30m));
            Assert.That(preWeakTargets.CashTarget, Is.EqualTo(0.58m));
            Assert.That(
                GetPreWeakGuardDrawdownThreshold(algorithm),
                Is.EqualTo(0.04m));
        }

        [Test]
        public void UsesConfiguredRuntimeSleeveTargets()
        {
            CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-growth-target"] = "0.18",
                ["pre-weak-def-target"] = "0.35",
                ["weak-growth-target"] = "0.05"
            });

            var weakTargets = QuantConnect.Algorithm.CSharp.StrategyConfig.GetSleeveTargets(
                QuantConnect.Algorithm.CSharp.RiskRegime.Weak);
            var preWeakTargets = QuantConnect.Algorithm.CSharp.StrategyConfig.PreWeakGuardSleeveTargets;

            Assert.That(weakTargets.GrowthTarget, Is.EqualTo(0.05m));
            Assert.That(weakTargets.DefensiveTarget, Is.EqualTo(0.40m));
            Assert.That(weakTargets.CashTarget, Is.EqualTo(0.55m));
            Assert.That(preWeakTargets.GrowthTarget, Is.EqualTo(0.18m));
            Assert.That(preWeakTargets.DefensiveTarget, Is.EqualTo(0.35m));
            Assert.That(preWeakTargets.CashTarget, Is.EqualTo(0.47m));
        }

        [Test]
        public void InvalidRuntimeSleeveTargetsFallBackToDefaults()
        {
            CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-growth-target"] = "0.80",
                ["pre-weak-def-target"] = "0.40",
                ["weak-growth-target"] = "0.70"
            });

            var weakTargets = QuantConnect.Algorithm.CSharp.StrategyConfig.GetSleeveTargets(
                QuantConnect.Algorithm.CSharp.RiskRegime.Weak);
            var preWeakTargets = QuantConnect.Algorithm.CSharp.StrategyConfig.PreWeakGuardSleeveTargets;

            Assert.That(weakTargets.GrowthTarget, Is.EqualTo(0.10m));
            Assert.That(weakTargets.CashTarget, Is.EqualTo(0.50m));
            Assert.That(preWeakTargets.GrowthTarget, Is.EqualTo(0.24m));
            Assert.That(preWeakTargets.DefensiveTarget, Is.EqualTo(0.30m));
            Assert.That(preWeakTargets.CashTarget, Is.EqualTo(0.46m));
        }

        [Test]
        public void EnablesPreWeakGuardByDefault()
        {
            var algorithm = CreateAlgorithm();

            Assert.That(IsPreWeakGuardEnabled(algorithm), Is.True);
        }

        [Test]
        public void DisablesPreWeakGuardWhenParameterIsFalse()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "false"
            });

            Assert.That(IsPreWeakGuardEnabled(algorithm), Is.False);
        }

        [Test]
        public void KeepsPreWeakGuardEnabledWhenParameterIsTrue()
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

            Assert.That(IsPreWeakGuardEnabled(algorithm), Is.True);
        }

        [Test]
        public void UsesConfiguredPreWeakGuardDrawdownThreshold()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "true",
                ["pre-weak-dd-threshold"] = "0.04"
            });

            Assert.That(GetPreWeakGuardDrawdownThreshold(algorithm), Is.EqualTo(0.04m));
        }

        [Test]
        public void PreWeakGuardDoesNotApplyWhenDisabled()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "false"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplyPreWeakGuard(algorithm, CreateNeutralDeterioratingSnapshot(), 94000m), Is.False);
        }

        [Test]
        public void PreWeakGuardDoesNotApplyInWeakRegime()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplyPreWeakGuard(algorithm, CreateWeakSnapshot(), 94000m), Is.False);
        }

        [Test]
        public void PreWeakGuardRequiresDrawdownThreshold()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplyPreWeakGuard(algorithm, CreateNeutralDeterioratingSnapshot(), 96000m), Is.False);
        }

        [Test]
        public void PreWeakGuardRequiresDeterioratingSignal()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplyPreWeakGuard(algorithm, CreateFavorableSnapshot(), 94000m), Is.False);
        }

        [Test]
        public void PreWeakGuardAppliesBeforeWeakWhenDrawdownAndSignalsDeteriorate()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["pre-weak-guard-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplyPreWeakGuard(algorithm, CreateNeutralDeterioratingSnapshot(), 94000m), Is.True);
        }

        [Test]
        public void UsesDefaultStressBandParameters()
        {
            CreateAlgorithm();

            Assert.That(
                QuantConnect.Algorithm.CSharp.StrategyConfig.WeakStressThreshold,
                Is.EqualTo(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultWeakStressThreshold));
            Assert.That(
                QuantConnect.Algorithm.CSharp.StrategyConfig.SevereStressThreshold,
                Is.EqualTo(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultSevereStressThreshold));
        }

        [Test]
        public void UsesPromotedOptStressDefaultParameters()
        {
            QuantConnect.Algorithm.CSharp.StrategyConfig.ResetRuntimeParameters();

            Assert.That(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultFavorableBreadthThreshold, Is.EqualTo(0.85m));
            Assert.That(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultWeakStressThreshold, Is.EqualTo(33m));
            Assert.That(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultSevereStressGap, Is.EqualTo(4m));
            Assert.That(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultSevereStressThreshold, Is.EqualTo(37m));
            Assert.That(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultGrowthAtrEligibilityLimit, Is.EqualTo(0.06m));
            Assert.That(QuantConnect.Algorithm.CSharp.StrategyConfig.FavorableBreadthThreshold, Is.EqualTo(0.85m));
            Assert.That(QuantConnect.Algorithm.CSharp.StrategyConfig.WeakStressThreshold, Is.EqualTo(33m));
            Assert.That(QuantConnect.Algorithm.CSharp.StrategyConfig.SevereStressGap, Is.EqualTo(4m));
            Assert.That(QuantConnect.Algorithm.CSharp.StrategyConfig.SevereStressThreshold, Is.EqualTo(37m));
            Assert.That(QuantConnect.Algorithm.CSharp.StrategyConfig.GrowthAtrEligibilityLimit, Is.EqualTo(0.06m));
        }

        [Test]
        public void UsesConfiguredStressBandParameters()
        {
            CreateAlgorithm(new Dictionary<string, string>
            {
                ["weak-stress-threshold"] = "32",
                ["severe-stress-gap"] = "4"
            });

            Assert.That(QuantConnect.Algorithm.CSharp.StrategyConfig.WeakStressThreshold, Is.EqualTo(32m));
            Assert.That(QuantConnect.Algorithm.CSharp.StrategyConfig.SevereStressThreshold, Is.EqualTo(36m));
        }

        [Test]
        public void InvalidStressBandFallsBackToDefaults()
        {
            CreateAlgorithm(new Dictionary<string, string>
            {
                ["weak-stress-threshold"] = "32",
                ["severe-stress-gap"] = "1"
            });

            Assert.That(
                QuantConnect.Algorithm.CSharp.StrategyConfig.WeakStressThreshold,
                Is.EqualTo(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultWeakStressThreshold));
            Assert.That(
                QuantConnect.Algorithm.CSharp.StrategyConfig.SevereStressThreshold,
                Is.EqualTo(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultSevereStressThreshold));
        }

        [Test]
        public void ComputedSevereStressAboveMaximumFallsBackToDefaults()
        {
            CreateAlgorithm(new Dictionary<string, string>
            {
                ["weak-stress-threshold"] = "40",
                ["severe-stress-gap"] = "8"
            });

            Assert.That(
                QuantConnect.Algorithm.CSharp.StrategyConfig.WeakStressThreshold,
                Is.EqualTo(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultWeakStressThreshold));
            Assert.That(
                QuantConnect.Algorithm.CSharp.StrategyConfig.SevereStressThreshold,
                Is.EqualTo(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultSevereStressThreshold));
        }

        [Test]
        public void RegimeModelUsesConfiguredSevereStressThreshold()
        {
            CreateAlgorithm(new Dictionary<string, string>
            {
                ["weak-stress-threshold"] = "32",
                ["severe-stress-gap"] = "4"
            });

            var neutralStress = new QuantConnect.Algorithm.CSharp.RegimeModel().Update(
                new QuantConnect.Algorithm.CSharp.RegimeInputs(100m, 100m, 100m, 0.50m, 31m));
            var weakStress = new QuantConnect.Algorithm.CSharp.RegimeModel().Update(
                new QuantConnect.Algorithm.CSharp.RegimeInputs(100m, 100m, 100m, 0.50m, 33m));
            var severeStress = new QuantConnect.Algorithm.CSharp.RegimeModel().Update(
                new QuantConnect.Algorithm.CSharp.RegimeInputs(100m, 100m, 100m, 0.50m, 36m));

            Assert.That(neutralStress.StressState, Is.EqualTo(QuantConnect.Algorithm.CSharp.SignalState.Neutral));
            Assert.That(neutralStress.SevereStress, Is.False);
            Assert.That(weakStress.StressState, Is.EqualTo(QuantConnect.Algorithm.CSharp.SignalState.Weak));
            Assert.That(weakStress.SevereStress, Is.False);
            Assert.That(severeStress.StressState, Is.EqualTo(QuantConnect.Algorithm.CSharp.SignalState.Weak));
            Assert.That(severeStress.SevereStress, Is.True);
        }

        [Test]
        public void DisablesSevereCrashOverrideByDefault()
        {
            var algorithm = CreateAlgorithm();

            Assert.That(IsSevereCrashOverrideEnabled(algorithm), Is.False);
        }

        [Test]
        public void EnablesSevereCrashOverrideWhenParameterIsTrue()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true"
            });

            Assert.That(IsSevereCrashOverrideEnabled(algorithm), Is.True);
        }

        [Test]
        public void IgnoresInvalidSevereCrashOverrideParameter()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "not-a-bool"
            });

            Assert.That(IsSevereCrashOverrideEnabled(algorithm), Is.False);
        }

        [Test]
        public void UsesConfiguredSevereCrashOverrideDrawdownThreshold()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true",
                ["sev-crash-dd-entry"] = "0.12"
            });

            Assert.That(GetSevereCrashOverrideDrawdownThreshold(algorithm), Is.EqualTo(0.12m));
        }

        [Test]
        public void IgnoresInvalidSevereCrashOverrideDrawdownThreshold()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true",
                ["sev-crash-dd-entry"] = "1.25"
            });

            Assert.That(
                GetSevereCrashOverrideDrawdownThreshold(algorithm),
                Is.EqualTo(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultSevereCrashOverrideDrawdownThreshold));
        }

        [Test]
        public void UsesConfiguredSevereCrashOverrideExitDrawdownThreshold()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true",
                ["sev-crash-dd-exit"] = "0.06"
            });

            Assert.That(GetSevereCrashOverrideExitDrawdownThreshold(algorithm), Is.EqualTo(0.06m));
        }

        [Test]
        public void IgnoresInvalidSevereCrashOverrideExitDrawdownThreshold()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true",
                ["sev-crash-dd-exit"] = "0.15"
            });

            Assert.That(
                GetSevereCrashOverrideExitDrawdownThreshold(algorithm),
                Is.EqualTo(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultSevereCrashOverrideExitDrawdownThreshold));
        }

        [Test]
        public void UsesConfiguredSevereCrashOverrideRecoveryConfirmationWeeks()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true",
                ["sev-crash-recovery-wks"] = "3"
            });

            Assert.That(GetSevereCrashOverrideRecoveryConfirmationWeeks(algorithm), Is.EqualTo(3));
        }

        [Test]
        public void IgnoresInvalidSevereCrashOverrideRecoveryConfirmationWeeks()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true",
                ["sev-crash-recovery-wks"] = "0"
            });

            Assert.That(
                GetSevereCrashOverrideRecoveryConfirmationWeeks(algorithm),
                Is.EqualTo(QuantConnect.Algorithm.CSharp.StrategyConfig.DefaultSevereCrashOverrideRecoveryConfirmationWeeks));
        }

        [Test]
        public void SevereCrashOverrideRequiresEnabledParameter()
        {
            var algorithm = CreateAlgorithm();
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplySevereCrashOverride(algorithm, CreateWeakSnapshot(), 89000m), Is.False);
        }

        [Test]
        public void SevereCrashOverrideRequiresWeakRegime()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplySevereCrashOverride(algorithm, CreateNeutralSevereSnapshot(), 89000m), Is.False);
        }

        [Test]
        public void SevereCrashOverrideRequiresSevereStress()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplySevereCrashOverride(algorithm, CreateWeakNonSevereSnapshot(), 89000m), Is.False);
        }

        [Test]
        public void SevereCrashOverrideRequiresTenPercentDrawdown()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(ShouldApplySevereCrashOverride(algorithm, CreateWeakSnapshot(), 91000m), Is.False);
        }

        [Test]
        public void SevereCrashOverrideRequiresAtLeastTwoWeakSignals()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(
                ShouldApplySevereCrashOverride(
                    algorithm,
                    CreateWeakSevereSnapshot(
                        QuantConnect.Algorithm.CSharp.SignalState.Weak,
                        QuantConnect.Algorithm.CSharp.SignalState.Neutral,
                        QuantConnect.Algorithm.CSharp.SignalState.Neutral),
                    89000m),
                Is.False);
        }

        [Test]
        public void SevereCrashOverrideAppliesWhenWeakSevereDrawdownAndTwoSignalsWeak()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(
                ShouldApplySevereCrashOverride(
                    algorithm,
                    CreateWeakSevereSnapshot(
                        QuantConnect.Algorithm.CSharp.SignalState.Weak,
                        QuantConnect.Algorithm.CSharp.SignalState.Weak,
                        QuantConnect.Algorithm.CSharp.SignalState.Neutral),
                    89000m),
                Is.True);
        }

        [Test]
        public void SevereCrashModeEntersWhenEntryConditionIsMet()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(UpdateSevereCrashMode(algorithm, CreateWeakSnapshot(), 89000m), Is.True);
            Assert.That(IsSevereCrashModeActive(algorithm), Is.True);
            Assert.That(GetSevereCrashModeState(algorithm), Is.EqualTo("enter"));
            Assert.That(GetSevereCrashRecoveryWeeks(algorithm), Is.EqualTo(0));
        }

        [Test]
        public void SevereCrashModeHoldsWhenRecoveryIsUnconfirmed()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(UpdateSevereCrashMode(algorithm, CreateWeakSnapshot(), 89000m), Is.True);
            Assert.That(UpdateSevereCrashMode(algorithm, CreateWeakNonSevereSnapshot(), 89000m), Is.True);

            Assert.That(IsSevereCrashModeActive(algorithm), Is.True);
            Assert.That(GetSevereCrashModeState(algorithm), Is.EqualTo("hold"));
            Assert.That(GetSevereCrashExitReason(algorithm), Is.EqualTo("none"));
        }

        [Test]
        public void SevereCrashModeExitsWhenDrawdownRecoversBelowExitThreshold()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(UpdateSevereCrashMode(algorithm, CreateWeakSnapshot(), 89000m), Is.True);
            Assert.That(UpdateSevereCrashMode(algorithm, CreateWeakNonSevereSnapshot(), 94000m), Is.False);

            Assert.That(IsSevereCrashModeActive(algorithm), Is.False);
            Assert.That(GetSevereCrashModeState(algorithm), Is.EqualTo("exit"));
            Assert.That(GetSevereCrashExitReason(algorithm), Is.EqualTo("drawdown-recovered"));
        }

        [Test]
        public void SevereCrashModeExitsAfterConsecutiveRecoveredRegimeWeeks()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["severe-crash-override-enabled"] = "true"
            });
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 100000m);

            Assert.That(UpdateSevereCrashMode(algorithm, CreateWeakSnapshot(), 89000m), Is.True);
            Assert.That(UpdateSevereCrashMode(algorithm, CreateNeutralSnapshot(), 89000m), Is.True);
            Assert.That(GetSevereCrashRecoveryWeeks(algorithm), Is.EqualTo(1));

            Assert.That(UpdateSevereCrashMode(algorithm, CreateNeutralSnapshot(), 89000m), Is.False);
            Assert.That(IsSevereCrashModeActive(algorithm), Is.False);
            Assert.That(GetSevereCrashModeState(algorithm), Is.EqualTo("exit"));
            Assert.That(GetSevereCrashExitReason(algorithm), Is.EqualTo("regime-recovered"));
            Assert.That(GetSevereCrashRecoveryWeeks(algorithm), Is.EqualTo(2));
        }

        [Test]
        public void BuildPersistedStateIncludesDefensiveRuntimeState()
        {
            var algorithm = CreateAlgorithm();
            SetPrivateField(algorithm, "_defensiveOverrideEquityHighWaterMark", 123456.78m);
            SetPrivateField(algorithm, "_severeCrashModeActive", true);
            SetPrivateField(algorithm, "_severeCrashRecoveryWeeks", 3);
            SetPrivateField(algorithm, "_severeCrashModeState", "hold");
            SetPrivateField(algorithm, "_severeCrashExitReason", "none");

            var state = BuildPersistedState(algorithm);

            Assert.That(state.DefensiveOverrideEquityHighWaterMark, Is.EqualTo(123456.78m));
            Assert.That(state.SevereCrashModeActive, Is.True);
            Assert.That(state.SevereCrashRecoveryWeeks, Is.EqualTo(3));
            Assert.That(state.SevereCrashModeState, Is.EqualTo("hold"));
            Assert.That(state.SevereCrashExitReason, Is.EqualTo("none"));
        }

        [Test]
        public void BuildPersistedStateIncludesDeploymentIdentity()
        {
            var algorithm = CreateAlgorithm();

            var state = BuildPersistedState(algorithm);

            Assert.That(state.AlgorithmVersion, Is.EqualTo(QuantConnect.Algorithm.CSharp.StrategyConfig.AlgorithmVersion));
            Assert.That(state.SourceRevision, Is.EqualTo(QuantConnect.Algorithm.CSharp.StrategyConfig.SourceRevision));
        }

        [Test]
        public void FormatsLiveHoldingsSummaryForDiagnostics()
        {
            var empty = FormatLiveHoldingsSummary(new Dictionary<string, decimal>());
            var holdings = FormatLiveHoldingsSummary(new Dictionary<string, decimal>
            {
                ["AVGO"] = 70m,
                ["AAPL"] = 101m
            });

            Assert.That(empty, Is.EqualTo("none"));
            Assert.That(holdings, Is.EqualTo("AAPL=101;AVGO=70"));
        }

        [Test]
        public void DefersStartupStateSaveWhenPersistedAndBrokerHoldingsAreEmpty()
        {
            var shouldDefer = ShouldDeferStartupStateSave(
                null,
                new Dictionary<string, decimal>(),
                new Dictionary<int, QuantConnect.Algorithm.CSharp.AegisOpenOrderState>());

            Assert.That(shouldDefer, Is.True);
        }

        [Test]
        public void DoesNotDeferStartupStateSaveWhenHoldingsAreVisible()
        {
            var shouldDefer = ShouldDeferStartupStateSave(
                null,
                new Dictionary<string, decimal>
                {
                    ["AAPL"] = 101m
                },
                new Dictionary<int, QuantConnect.Algorithm.CSharp.AegisOpenOrderState>());

            Assert.That(shouldDefer, Is.False);
        }

        [Test]
        public void DefersStartupStateSaveWhenPersistedHoldingsExistAndBrokerHoldingsAreMissing()
        {
            var state = new QuantConnect.Algorithm.CSharp.AegisLiveState
            {
                SchemaVersion = QuantConnect.Algorithm.CSharp.StrategyConfig.LiveStateSchemaVersion,
                BrokerHoldingsByTicker = new Dictionary<string, decimal>
                {
                    ["AAPL"] = 101m
                }
            };

            var shouldDefer = ShouldDeferStartupStateSave(
                state,
                new Dictionary<string, decimal>(),
                new Dictionary<int, QuantConnect.Algorithm.CSharp.AegisOpenOrderState>());

            Assert.That(shouldDefer, Is.True);
        }

        [Test]
        public void FormatsStartupReconciliationMessageForDeferredBrokerSnapshot()
        {
            var message = FormatStartupReconciliationMessage(
                brokerHoldingsCount: 0,
                persistedHoldingsCount: 7,
                brokerOpenOrdersCount: 0,
                persistedOpenOrdersCount: 0,
                holdingsMatch: false,
                openOrdersMatch: true,
                shouldDeferStartupSave: true);

            Assert.That(message, Does.Contain("broker snapshot not ready"));
            Assert.That(message, Does.Contain("persisted state retained"));
            Assert.That(message, Does.Not.Contain("Broker state wins"));
        }

        [Test]
        public void FormatsStartupReconciliationMessageWhenBrokerStateWins()
        {
            var message = FormatStartupReconciliationMessage(
                brokerHoldingsCount: 7,
                persistedHoldingsCount: 6,
                brokerOpenOrdersCount: 0,
                persistedOpenOrdersCount: 0,
                holdingsMatch: false,
                openOrdersMatch: true,
                shouldDeferStartupSave: false);

            Assert.That(message, Does.Contain("broker/store mismatch detected"));
            Assert.That(message, Does.Contain("Broker state wins"));
        }

        [Test]
        public void FormatsStressDiagnosticWhenStressWindowIsNotReady()
        {
            var diagnostic = FormatStressDiagnostic(
                "WarmupFinished",
                "VIX",
                stressCount: 0,
                latestStressClose: null,
                stressAverage5: null,
                isReady: false);

            Assert.That(diagnostic, Does.Contain("[AEGIS-STRESS-DIAG]"));
            Assert.That(diagnostic, Does.Contain("Phase=WarmupFinished"));
            Assert.That(diagnostic, Does.Contain("StressSymbol=VIX"));
            Assert.That(diagnostic, Does.Contain("StressCount=0"));
            Assert.That(diagnostic, Does.Contain("LatestStressClose=none"));
            Assert.That(diagnostic, Does.Contain("StressAverage5=none"));
            Assert.That(diagnostic, Does.Contain("StressReady=False"));
        }

        [Test]
        public void FormatsStressDiagnosticWhenStressWindowIsReady()
        {
            var diagnostic = FormatStressDiagnostic(
                "WeeklyReview",
                "VIX",
                stressCount: 5,
                latestStressClose: 18.25m,
                stressAverage5: 19.75m,
                isReady: true);

            Assert.That(diagnostic, Does.Contain("Phase=WeeklyReview"));
            Assert.That(diagnostic, Does.Contain("StressCount=5"));
            Assert.That(diagnostic, Does.Contain("LatestStressClose=18.25"));
            Assert.That(diagnostic, Does.Contain("StressAverage5=19.75"));
            Assert.That(diagnostic, Does.Contain("StressReady=True"));
        }

        [Test]
        public void DoesNotDeferStartupStateSaveWhenPersistedAndBrokerHoldingsAreBothVisible()
        {
            var state = new QuantConnect.Algorithm.CSharp.AegisLiveState
            {
                SchemaVersion = QuantConnect.Algorithm.CSharp.StrategyConfig.LiveStateSchemaVersion,
                BrokerHoldingsByTicker = new Dictionary<string, decimal>
                {
                    ["AAPL"] = 101m
                }
            };

            var shouldDefer = ShouldDeferStartupStateSave(
                state,
                new Dictionary<string, decimal>
                {
                    ["AAPL"] = 101m
                },
                new Dictionary<int, QuantConnect.Algorithm.CSharp.AegisOpenOrderState>());

            Assert.That(shouldDefer, Is.False);
        }

        [Test]
        public void RestorePersistedRuntimeStateRestoresDefensiveRuntimeState()
        {
            var algorithm = CreateAlgorithm();
            var state = new QuantConnect.Algorithm.CSharp.AegisLiveState
            {
                SchemaVersion = QuantConnect.Algorithm.CSharp.StrategyConfig.LiveStateSchemaVersion,
                DefensiveOverrideEquityHighWaterMark = 98765.43m,
                SevereCrashModeActive = true,
                SevereCrashRecoveryWeeks = 2,
                SevereCrashModeState = "hold",
                SevereCrashExitReason = "none"
            };

            RestorePersistedRuntimeState(algorithm, state);

            Assert.That(GetDefensiveOverrideEquityHighWaterMark(algorithm), Is.EqualTo(98765.43m));
            Assert.That(IsSevereCrashModeActive(algorithm), Is.True);
            Assert.That(GetSevereCrashRecoveryWeeks(algorithm), Is.EqualTo(2));
            Assert.That(GetSevereCrashModeState(algorithm), Is.EqualTo("hold"));
            Assert.That(GetSevereCrashExitReason(algorithm), Is.EqualTo("none"));
        }

        [Test]
        public void LiveStateFingerprintChangesWhenDefensiveRuntimeStateChanges()
        {
            var baseline = new QuantConnect.Algorithm.CSharp.AegisLiveState
            {
                SchemaVersion = QuantConnect.Algorithm.CSharp.StrategyConfig.LiveStateSchemaVersion,
                DefensiveOverrideEquityHighWaterMark = 100000m,
                SevereCrashModeActive = true,
                SevereCrashRecoveryWeeks = 1,
                SevereCrashModeState = "hold",
                SevereCrashExitReason = "none"
            };
            var changed = new QuantConnect.Algorithm.CSharp.AegisLiveState
            {
                SchemaVersion = QuantConnect.Algorithm.CSharp.StrategyConfig.LiveStateSchemaVersion,
                DefensiveOverrideEquityHighWaterMark = 99000m,
                SevereCrashModeActive = true,
                SevereCrashRecoveryWeeks = 1,
                SevereCrashModeState = "hold",
                SevereCrashExitReason = "none"
            };

            Assert.That(BuildLiveStateFingerprint(changed), Is.Not.EqualTo(BuildLiveStateFingerprint(baseline)));
        }

        [Test]
        public void LiveStateFingerprintChangesWhenDeploymentIdentityChanges()
        {
            var baseline = new QuantConnect.Algorithm.CSharp.AegisLiveState
            {
                SchemaVersion = QuantConnect.Algorithm.CSharp.StrategyConfig.LiveStateSchemaVersion,
                AlgorithmVersion = "AegisGrowthAllocation-2026-05-23-optstress-defaults",
                SourceRevision = "revision-a"
            };
            var changed = new QuantConnect.Algorithm.CSharp.AegisLiveState
            {
                SchemaVersion = QuantConnect.Algorithm.CSharp.StrategyConfig.LiveStateSchemaVersion,
                AlgorithmVersion = "AegisGrowthAllocation-2026-05-23-optstress-defaults",
                SourceRevision = "revision-b"
            };

            Assert.That(BuildLiveStateFingerprint(changed), Is.Not.EqualTo(BuildLiveStateFingerprint(baseline)));
        }

        [Test]
        public void PortfolioManagerAppliesSevereCrashOverrideSleeves()
        {
            var growthSymbol = QuantConnect.Symbol.Create("AAPL", QuantConnect.SecurityType.Equity, QuantConnect.Market.USA);
            var defensiveSymbol = QuantConnect.Symbol.Create("SGOV", QuantConnect.SecurityType.Equity, QuantConnect.Market.USA);
            var plan = BuildWeakPlan(
                growthSymbol,
                defensiveSymbol,
                QuantConnect.Algorithm.CSharp.StrategyConfig.SevereCrashOverrideSleeveTargets);

            Assert.That(plan.SelectedGrowthSymbols, Is.Empty);
            Assert.That(plan.TargetWeights[growthSymbol], Is.EqualTo(0m));
            Assert.That(plan.TargetWeights[defensiveSymbol], Is.EqualTo(0.20m));
            Assert.That(1m - plan.TargetWeights.Values.Sum(), Is.EqualTo(0.80m));
        }

        [Test]
        public void FormatsOverrideAttributionDiagnostics()
        {
            var algorithm = CreateAlgorithm();

            var diagnostics = FormatOverrideDiagnostics(
                algorithm,
                preWeakGuardActive: true,
                severeCrashOverrideActive: true,
                sleeveOverride: "severe-crash",
                overrideReason: "weak-severe-dd10-signals2",
                drawdownFromHigh: 0.1234m,
                baseSleeveTargets: QuantConnect.Algorithm.CSharp.StrategyConfig.SleeveTargetsByRegime[QuantConnect.Algorithm.CSharp.RiskRegime.Weak],
                finalSleeveTargets: QuantConnect.Algorithm.CSharp.StrategyConfig.SevereCrashOverrideSleeveTargets,
                severeCrashModeState: "hold",
                severeCrashRecoveryWeeks: 1,
                severeCrashExitReason: "none");

            Assert.That(diagnostics, Does.Contain("PreWeakGuardActive=True"));
            Assert.That(diagnostics, Does.Contain("SevereCrashOverrideActive=True"));
            Assert.That(diagnostics, Does.Contain("SleeveOverride=severe-crash"));
            Assert.That(diagnostics, Does.Contain("OverrideReason=weak-severe-dd10-signals2"));
            Assert.That(diagnostics, Does.Contain("DrawdownFromHigh=0.1234"));
            Assert.That(diagnostics, Does.Contain("BaseTarget=G0.1000/D0.4000/C0.5000"));
            Assert.That(diagnostics, Does.Contain("FinalTarget=G0.0000/D0.2000/C0.8000"));
            Assert.That(diagnostics, Does.Contain("SevereCrashModeState=hold"));
            Assert.That(diagnostics, Does.Contain("SevereCrashRecoveryWeeks=1"));
            Assert.That(diagnostics, Does.Contain("SevereCrashExitReason=none"));
        }

        [Test]
        public void FormatsCompactCrisisDiagnosticSummaryWithForwardReturns()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["crisis-diagnostics"] = "true"
            });

            RecordCrisisDiagnosticObservation(
                algorithm,
                new DateTime(2022, 1, 3),
                100000m,
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                preWeakGuardActive: true,
                severeCrashOverrideActive: false,
                drawdownFromHigh: 0.05m,
                finalSleeveTargets: QuantConnect.Algorithm.CSharp.StrategyConfig.PreWeakGuardSleeveTargets);
            RecordCrisisDiagnosticObservation(
                algorithm,
                new DateTime(2022, 1, 10),
                101000m,
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                preWeakGuardActive: false,
                severeCrashOverrideActive: false,
                drawdownFromHigh: 0.01m,
                finalSleeveTargets: QuantConnect.Algorithm.CSharp.StrategyConfig.SleeveTargetsByRegime[QuantConnect.Algorithm.CSharp.RiskRegime.Neutral]);
            RecordCrisisDiagnosticObservation(
                algorithm,
                new DateTime(2022, 1, 17),
                102000m,
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                preWeakGuardActive: false,
                severeCrashOverrideActive: false,
                drawdownFromHigh: 0.02m,
                finalSleeveTargets: QuantConnect.Algorithm.CSharp.StrategyConfig.SleeveTargetsByRegime[QuantConnect.Algorithm.CSharp.RiskRegime.Neutral]);
            RecordCrisisDiagnosticObservation(
                algorithm,
                new DateTime(2022, 1, 24),
                103000m,
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                preWeakGuardActive: false,
                severeCrashOverrideActive: false,
                drawdownFromHigh: 0.01m,
                finalSleeveTargets: QuantConnect.Algorithm.CSharp.StrategyConfig.SleeveTargetsByRegime[QuantConnect.Algorithm.CSharp.RiskRegime.Neutral]);
            RecordCrisisDiagnosticObservation(
                algorithm,
                new DateTime(2022, 1, 31),
                104000m,
                QuantConnect.Algorithm.CSharp.RiskRegime.Favorable,
                preWeakGuardActive: false,
                severeCrashOverrideActive: true,
                drawdownFromHigh: 0.12m,
                finalSleeveTargets: QuantConnect.Algorithm.CSharp.StrategyConfig.SevereCrashOverrideSleeveTargets);

            var summary = FormatCompactCrisisDiagnosticSummary(algorithm);

            Assert.That(summary, Does.Contain("[AEGIS-DIAG-SUMMARY]"));
            Assert.That(summary, Does.Contain("Weeks=5"));
            Assert.That(summary, Does.Contain("PreWeakWeeks=1"));
            Assert.That(summary, Does.Contain("SevereCrashWeeks=1"));
            Assert.That(summary, Does.Contain("PreWeakAvgDrawdown=0.0500"));
            Assert.That(summary, Does.Contain("PreWeakNextReturnAvg=0.0100"));
            Assert.That(summary, Does.Contain("PreWeakFwd4Avg=0.0400"));
            Assert.That(summary, Does.Contain("NonPreWeakWeeks=4"));
        }

        [Test]
        public void FormatsCompactCrisisDiagnosticSummaryWhenNoObservationsExist()
        {
            var algorithm = CreateAlgorithm(new Dictionary<string, string>
            {
                ["crisis-diagnostics"] = "true"
            });

            var summary = FormatCompactCrisisDiagnosticSummary(algorithm);

            Assert.That(summary, Does.Contain("[AEGIS-DIAG-SUMMARY]"));
            Assert.That(summary, Does.Contain("Weeks=0"));
            Assert.That(summary, Does.Contain("PreWeakWeeks=0"));
            Assert.That(summary, Does.Contain("SevereCrashWeeks=0"));
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

        private static bool IsSevereCrashOverrideEnabled(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField("_severeCrashOverrideEnabled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (bool)field.GetValue(algorithm);
        }

        private static decimal GetSevereCrashOverrideDrawdownThreshold(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField("_severeCrashOverrideDrawdownThreshold", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (decimal)field.GetValue(algorithm);
        }

        private static decimal GetSevereCrashOverrideExitDrawdownThreshold(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField("_severeCrashOverrideExitDrawdownThreshold", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (decimal)field.GetValue(algorithm);
        }

        private static int GetSevereCrashOverrideRecoveryConfirmationWeeks(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField("_severeCrashOverrideRecoveryConfirmationWeeks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (int)field.GetValue(algorithm);
        }

        private static bool IsSevereCrashModeActive(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField("_severeCrashModeActive", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (bool)field.GetValue(algorithm);
        }

        private static int GetSevereCrashRecoveryWeeks(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField("_severeCrashRecoveryWeeks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (int)field.GetValue(algorithm);
        }

        private static string GetSevereCrashModeState(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField("_severeCrashModeState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (string)field.GetValue(algorithm);
        }

        private static string GetSevereCrashExitReason(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField("_severeCrashExitReason", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (string)field.GetValue(algorithm);
        }

        private static decimal GetDefensiveOverrideEquityHighWaterMark(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var field = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetField("_defensiveOverrideEquityHighWaterMark", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            return (decimal)field.GetValue(algorithm);
        }

        private static QuantConnect.Algorithm.CSharp.AegisLiveState BuildPersistedState(
            QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var method = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetMethod("BuildPersistedState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            return (QuantConnect.Algorithm.CSharp.AegisLiveState)method.Invoke(algorithm, Array.Empty<object>());
        }

        private static void RestorePersistedRuntimeState(
            QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm,
            QuantConnect.Algorithm.CSharp.AegisLiveState state)
        {
            var method = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetMethod("RestorePersistedRuntimeState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            method.Invoke(algorithm, new object[] { state });
        }

        private static string BuildLiveStateFingerprint(QuantConnect.Algorithm.CSharp.AegisLiveState state)
        {
            var method = typeof(QuantConnect.Algorithm.CSharp.LiveStateStore)
                .GetMethod("BuildFingerprint", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(null, new object[] { state });
        }

        private static string FormatLiveHoldingsSummary(IReadOnlyDictionary<string, decimal> holdings)
        {
            var method = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetMethod("FormatLiveHoldingsSummary", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(null, new object[] { holdings });
        }

        private static bool ShouldDeferStartupStateSave(
            QuantConnect.Algorithm.CSharp.AegisLiveState persistedState,
            IReadOnlyDictionary<string, decimal> brokerHoldings,
            IReadOnlyDictionary<int, QuantConnect.Algorithm.CSharp.AegisOpenOrderState> brokerOpenOrders)
        {
            var method = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetMethod("ShouldDeferStartupStateSave", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(null, new object[] { persistedState, brokerHoldings, brokerOpenOrders });
        }

        private static string FormatStartupReconciliationMessage(
            int brokerHoldingsCount,
            int persistedHoldingsCount,
            int brokerOpenOrdersCount,
            int persistedOpenOrdersCount,
            bool holdingsMatch,
            bool openOrdersMatch,
            bool shouldDeferStartupSave)
        {
            var method = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetMethod("FormatStartupReconciliationMessage", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(
                null,
                new object[]
                {
                    brokerHoldingsCount,
                    persistedHoldingsCount,
                    brokerOpenOrdersCount,
                    persistedOpenOrdersCount,
                    holdingsMatch,
                    openOrdersMatch,
                    shouldDeferStartupSave
                });
        }

        private static string FormatStressDiagnostic(
            string phase,
            string stressSymbol,
            int stressCount,
            decimal? latestStressClose,
            decimal? stressAverage5,
            bool isReady)
        {
            var method = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetMethod("FormatStressDiagnostic", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(
                null,
                new object[]
                {
                    phase,
                    stressSymbol,
                    stressCount,
                    latestStressClose,
                    stressAverage5,
                    isReady
                });
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

        private static bool ShouldApplySevereCrashOverride(
            QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm,
            QuantConnect.Algorithm.CSharp.RegimeSnapshot regimeSnapshot,
            decimal totalPortfolioValue)
        {
            var method = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetMethod("ShouldApplySevereCrashOverride", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(algorithm, new object[] { regimeSnapshot, totalPortfolioValue });
        }

        private static bool UpdateSevereCrashMode(
            QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm,
            QuantConnect.Algorithm.CSharp.RegimeSnapshot regimeSnapshot,
            decimal totalPortfolioValue)
        {
            var method = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetMethod("UpdateSevereCrashMode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            return (bool)method.Invoke(algorithm, new object[] { regimeSnapshot, totalPortfolioValue });
        }

        private static string FormatOverrideDiagnostics(
            QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm,
            bool preWeakGuardActive,
            bool severeCrashOverrideActive,
            string sleeveOverride,
            string overrideReason,
            decimal drawdownFromHigh,
            QuantConnect.Algorithm.CSharp.SleeveTargets baseSleeveTargets,
            QuantConnect.Algorithm.CSharp.SleeveTargets finalSleeveTargets,
            string severeCrashModeState = "none",
            int severeCrashRecoveryWeeks = 0,
            string severeCrashExitReason = "none")
        {
            var method = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetMethod("FormatOverrideDiagnostics", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(
                algorithm,
                new object[]
                {
                    preWeakGuardActive,
                    severeCrashOverrideActive,
                    sleeveOverride,
                    overrideReason,
                    drawdownFromHigh,
                    baseSleeveTargets,
                    finalSleeveTargets,
                    severeCrashModeState,
                    severeCrashRecoveryWeeks,
                    severeCrashExitReason
                });
        }

        private static void RecordCrisisDiagnosticObservation(
            QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm,
            DateTime date,
            decimal equity,
            QuantConnect.Algorithm.CSharp.RiskRegime activeRegime,
            bool preWeakGuardActive,
            bool severeCrashOverrideActive,
            decimal drawdownFromHigh,
            QuantConnect.Algorithm.CSharp.SleeveTargets finalSleeveTargets)
        {
            var method = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetMethod("RecordCrisisDiagnosticObservation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            method.Invoke(
                algorithm,
                new object[]
                {
                    date,
                    equity,
                    activeRegime,
                    preWeakGuardActive,
                    severeCrashOverrideActive,
                    drawdownFromHigh,
                    finalSleeveTargets
                });
        }

        private static string FormatCompactCrisisDiagnosticSummary(
            QuantConnect.Algorithm.CSharp.AegisGrowthAllocation algorithm)
        {
            var method = typeof(QuantConnect.Algorithm.CSharp.AegisGrowthAllocation)
                .GetMethod("FormatCompactCrisisDiagnosticSummary", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(algorithm, Array.Empty<object>());
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

        private static QuantConnect.Algorithm.CSharp.RegimeSnapshot CreateNeutralSnapshot()
        {
            return new QuantConnect.Algorithm.CSharp.RegimeSnapshot(
                QuantConnect.Algorithm.CSharp.RiskRegime.Weak,
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                QuantConnect.Algorithm.CSharp.SignalState.Neutral,
                QuantConnect.Algorithm.CSharp.SignalState.Neutral,
                QuantConnect.Algorithm.CSharp.SignalState.Neutral,
                severeStress: false,
                upgradeConfirmationCount: 0);
        }

        private static QuantConnect.Algorithm.CSharp.RegimeSnapshot CreateWeakNonSevereSnapshot()
        {
            return new QuantConnect.Algorithm.CSharp.RegimeSnapshot(
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                QuantConnect.Algorithm.CSharp.RiskRegime.Weak,
                QuantConnect.Algorithm.CSharp.RiskRegime.Weak,
                QuantConnect.Algorithm.CSharp.SignalState.Weak,
                QuantConnect.Algorithm.CSharp.SignalState.Weak,
                QuantConnect.Algorithm.CSharp.SignalState.Weak,
                severeStress: false,
                upgradeConfirmationCount: 0);
        }

        private static QuantConnect.Algorithm.CSharp.RegimeSnapshot CreateWeakSevereSnapshot(
            QuantConnect.Algorithm.CSharp.SignalState trendState,
            QuantConnect.Algorithm.CSharp.SignalState breadthState,
            QuantConnect.Algorithm.CSharp.SignalState stressState)
        {
            return new QuantConnect.Algorithm.CSharp.RegimeSnapshot(
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                QuantConnect.Algorithm.CSharp.RiskRegime.Weak,
                QuantConnect.Algorithm.CSharp.RiskRegime.Weak,
                trendState,
                breadthState,
                stressState,
                severeStress: true,
                upgradeConfirmationCount: 0);
        }

        private static QuantConnect.Algorithm.CSharp.RegimeSnapshot CreateNeutralSevereSnapshot()
        {
            return new QuantConnect.Algorithm.CSharp.RegimeSnapshot(
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
                QuantConnect.Algorithm.CSharp.RiskRegime.Neutral,
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
