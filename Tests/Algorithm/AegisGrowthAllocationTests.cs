using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Lean.Engine.DataFeeds;
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
