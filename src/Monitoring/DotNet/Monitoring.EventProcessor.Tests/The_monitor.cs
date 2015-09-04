using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.Monitoring.EventProcessor;
using Microsoft.Reactive.Testing;
using Microsoft.ServiceBus.Messaging;
using Xunit;

namespace Monitoring.EventProcessor.Tests
{
    public class The_monitor
    {
        [Fact]
        public void ShouldEmitAnEventForEachPartition()
        {
            var scheduler = new TestScheduler();
            var captured = new List<EventEntry>();

            var partitionIds = new[] { "0", "1", "2" };

            var m = new PartitionMonitor(
                partitionIds,
                partitionId => Task.FromResult(new PartitionCheckpoint()), 
                partitionId => Task.FromResult(new PartitionDescription("myhub", partitionId)),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                scheduler);

            using (m.Subscribe(captured.Add))
            {
                var enoughForAllPartitions = TimeSpan.FromSeconds(partitionIds.Length);
                scheduler.AdvanceBy(enoughForAllPartitions.Ticks);
            }

            Assert.Equal(partitionIds.Length, captured.Count);

            for (int i = 0; i < captured.Count; i++)
            {
                Assert.Equal(partitionIds[i], captured[i].PartitionId);
            }
        }
    }
}
