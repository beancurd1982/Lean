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
