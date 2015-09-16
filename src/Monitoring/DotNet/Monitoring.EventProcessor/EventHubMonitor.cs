using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor
{
    public class EventHubMonitor : IObservable<PartitionSnapshot>
    {
        private readonly Func<string, Task<PartitionCheckpoint>> _getLastCheckpointAsync;
        private readonly Func<string, Task<PartitionDescription>> _getEventHubPartitionAsync;
        private readonly ISubject<PartitionSnapshot> _replay = new ReplaySubject<PartitionSnapshot>();

        public EventHubMonitor(
                string[] partitionIds,
                Func<string, Task<PartitionCheckpoint>> getLastCheckpointAsync,
                Func<string, Task<PartitionDescription>> getEventHubPartitionAsync,
                TimeSpan betweenEachPartition,
                TimeSpan afterAllPartitions,
                IScheduler scheduler = null)
        {
            _getLastCheckpointAsync = getLastCheckpointAsync;
            _getEventHubPartitionAsync = getEventHubPartitionAsync;

            var stream = GenerateStream(
                partitionIds, 
                betweenEachPartition, 
                afterAllPartitions,
                scheduler ?? DefaultScheduler.Instance);

            stream.Subscribe(_replay);
        }

        private IObservable<PartitionSnapshot> GenerateStream(
            string[] partitionIds,
            TimeSpan delayBetweenEachPartition,
            TimeSpan delayBetweenPartitionSet,
            IScheduler scheduler)
        {
            var lastIndex = partitionIds.Length - 1;

            var firstTime = true;
            Func<int, TimeSpan> timeSelector = index =>
            {
                if (firstTime)
                {
                    firstTime = false;
                    return TimeSpan.Zero;
                }

                return index != 0
                    ? delayBetweenEachPartition
                    : delayBetweenPartitionSet;
            };

            return Observable.Generate(
                initialState: 0,
                condition: _ => true, // never terminate
                iterate: index => index < lastIndex ? index + 1 : 0,
                resultSelector: index => partitionIds[index],
                timeSelector: timeSelector,
                scheduler: scheduler
                )
                .SelectMany(partitionId => Calculate(partitionId).ToObservable());
        }

        public async Task<PartitionSnapshot> Calculate(string partitionId)
        {
            var partition = await _getEventHubPartitionAsync(partitionId).ConfigureAwait(false);
            var checkpoint = await _getLastCheckpointAsync(partitionId).ConfigureAwait(false);

            return new PartitionSnapshot
            {
                PartitionId = partitionId,
                UnprocessedEvents = partition.EndSequenceNumber - checkpoint.SequenceNumber,
                EndSequenceNumber = partition.EndSequenceNumber,
                LastEnqueuedTimeUtc = partition.LastEnqueuedTimeUtc,
                LastCheckpointTimeUtc = checkpoint.LastCheckpointTimeUtc,
                RecordedAtTimeUtc = DateTimeOffset.UtcNow
            };

        }

        public IDisposable Subscribe(IObserver<PartitionSnapshot> observer)
        {
            return _replay.Subscribe(observer);
        }
    }
}