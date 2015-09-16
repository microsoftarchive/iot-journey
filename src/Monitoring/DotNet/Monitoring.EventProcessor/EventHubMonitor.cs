using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly string[] _partitionIds;
        private readonly Func<string, Task<PartitionCheckpoint>> _getLastCheckpointAsync;
        private readonly Func<string, Task<PartitionDescription>> _getEventHubPartitionAsync;
        private readonly IScheduler _scheduler;
        private readonly TimeSpan _betweenEachPartition;
        private readonly TimeSpan _afterAllPartitions;
        private readonly ISubject<PartitionSnapshot> _replay = new ReplaySubject<PartitionSnapshot>();

        public EventHubMonitor(
                string[] partitionIds,
                Func<string, Task<PartitionCheckpoint>> getLastCheckpointAsync,
                Func<string, Task<PartitionDescription>> getEventHubPartitionAsync,
                TimeSpan betweenEachPartition,
                TimeSpan afterAllPartitions,
                IScheduler scheduler = null)
        {
            _partitionIds = partitionIds;
            _getLastCheckpointAsync = getLastCheckpointAsync;
            _getEventHubPartitionAsync = getEventHubPartitionAsync;
            _betweenEachPartition = betweenEachPartition;
            _afterAllPartitions = afterAllPartitions;

            // We allow a scheduler to be passed in for testing purposes
            _scheduler = scheduler ?? DefaultScheduler.Instance;

            GenerateStream().Subscribe(_replay);
        }

        private IObservable<PartitionSnapshot> GenerateStream()
        {
            var delayBetweenEachPartition = _betweenEachPartition;
            var delayBetweenPartitionSet = _afterAllPartitions;

            var previousSnapshots = _partitionIds
                .ToDictionary(partitionId => partitionId, partitionId => new PartitionSnapshot());

            var lastIndex = _partitionIds.Length - 1;

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
                resultSelector: index => _partitionIds[index],
                timeSelector: timeSelector,
                scheduler: _scheduler
                )
                .SelectMany(partitionId => Calculate(partitionId, previousSnapshots).ToObservable());
        }

        public async Task<PartitionSnapshot> Calculate(
            string partitionId,
            IDictionary<string, PartitionSnapshot> previousSnapshots)
        {
            var past = previousSnapshots[partitionId];

            PartitionDescription partition;
            PartitionCheckpoint checkpoint;

            try
            {
                partition = await _getEventHubPartitionAsync(partitionId).ConfigureAwait(false);

                checkpoint = await _getLastCheckpointAsync(partitionId).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                past.IsStale = true;
                return past;
            }

            var current = new PartitionSnapshot
            {
                PartitionId = partitionId,
                UnprocessedEvents = partition.EndSequenceNumber - checkpoint.SequenceNumber,
                EndSequenceNumber = partition.EndSequenceNumber,
                LastEnqueuedTimeUtc = partition.LastEnqueuedTimeUtc,
                LastCheckpointTimeUtc = checkpoint.LastCheckpointTimeUtc,
                RecordedAtTimeUtc = DateTimeOffset.UtcNow
            };

            // store for the next iteration
            previousSnapshots[partitionId] = current;
            return current;
        }

        public IDisposable Subscribe(IObserver<PartitionSnapshot> observer)
        {
            return _replay.Subscribe(observer);
        }
    }
}