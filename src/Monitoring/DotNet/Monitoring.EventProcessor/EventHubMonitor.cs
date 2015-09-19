// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private static readonly Predicate<Exception> ExceptionsToIgnore = e =>
        {
            if (e is TimeoutException) return true;
            if (e is MessagingCommunicationException) return true;

            return false;
        };

        private readonly Func<string, Task<PartitionDescription>> _getEventHubPartitionAsync;
        private readonly Func<string, Task<PartitionCheckpoint>> _getLastCheckpointAsync;
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

            // The stream will begin producing values
            // as soon as we subcribe.
            var stream = GenerateStream(
                partitionIds,
                betweenEachPartition,
                afterAllPartitions,
                scheduler ?? DefaultScheduler.Instance);

            // Since we are subscribing inside the constructor
            // this means that even if no external consumers 
            // subscribe, we will still be querying the event
            // hub. Since we are using a `ReplaySubject` subject
            // all of the values are stored and replayed for 
            // any subscribers of `_replay`.
            stream.Subscribe(_replay);
        }

        public IDisposable Subscribe(IObserver<PartitionSnapshot> observer)
        {
            return _replay.Subscribe(observer);
        }

        private IObservable<PartitionSnapshot> GenerateStream(
            string[] partitionIds,
            TimeSpan delayBetweenEachPartition,
            TimeSpan delayBeforeRequeryingPartition,
            IScheduler scheduler)
        {
            // Create a sequence of the partition ids
            // staggered so that we don't hit them all
            // at the same time.
            var partitions = Observable.Generate(
                0,
                i => i < partitionIds.Length,
                i => i + 1,
                i => partitionIds[i],
                _ => delayBetweenEachPartition,
                scheduler);

            var actualDelay = TimeSpan.FromTicks(delayBetweenEachPartition.Ticks*partitionIds.Length)
                              + delayBeforeRequeryingPartition;

            return partitions.SelectMany(id =>
            {
                var timeDelayedRepeatingQuery = Observable
                    .Interval(actualDelay, scheduler)
                    .Select(_ => id);

                return Observable
                    .Return(id)
                    .Concat(timeDelayedRepeatingQuery)
                    .SelectMany(CaptureSnapshotSafely);
            });
        }

        private IObservable<PartitionSnapshot> CaptureSnapshotSafely(string partitionId)
        {
            return CaptureSnapshotAsync(partitionId)
                .ToObservable()
                .Catch(
                    (Exception e) => 
                        ExceptionsToIgnore(e)
                            ? Observable.Empty<PartitionSnapshot>()
                            : Observable.Throw<PartitionSnapshot>(e));
        }

        public async Task<PartitionSnapshot> CaptureSnapshotAsync(string partitionId)
        {
            var partition = await _getEventHubPartitionAsync(partitionId).ConfigureAwait(false);
            var checkpoint = await _getLastCheckpointAsync(partitionId).ConfigureAwait(false);

            return new PartitionSnapshot
            {
                PartitionId = partitionId,
                EndSequenceNumber = partition.EndSequenceNumber,
                LastEnqueuedTimeUtc = partition.LastEnqueuedTimeUtc,
                LastCheckpointSequenceNumber = checkpoint.SequenceNumber,
                LastCheckpointTimeUtc = checkpoint.LastCheckpointTimeUtc,
                CapturedAtTimeUtc = DateTimeOffset.UtcNow,
                LastEnqueuedOffset = partition.LastEnqueuedOffset,
                IncomingBytesPerSecond = partition.IncomingBytesPerSecond,
                OutgoingBytesPerSecond = partition.OutgoingBytesPerSecond
            };
        }
    }
}