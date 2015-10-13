// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.Monitoring.EventProcessor;
using Microsoft.ServiceBus.Messaging;
using Xunit;

namespace Monitoring.EventProcessor.Tests
{
    public class The_snapshot_of_a_partition
    {
        private const string PartitionId = "0";
        private readonly PartitionSnapshot _snapshot;
        private readonly int _howFarBehindForCheckpoint;
        private readonly int _howFarBehindForPreviousEnqueue;
        private readonly DateTime _lastCheckpointTimeUtc;
        private readonly DateTime _lastEnqueuedTimeUtc;
        private readonly string _lastEnqueuedOffset;

        private readonly long _incomingByteRate;
        private readonly long _outgoingByteRate;

        private readonly long _sequenceNumberOfMostRecentEvent = 10000L;
        private readonly TimeSpan _timeBetweenCheckpoints;
        private readonly TimeSpan _timeBetweenLastEnqueueAndPreviousEnqueue;

        public The_snapshot_of_a_partition()
        {
            var timeBetweenLastEnqueueAndLastCheckpoint = TimeSpan.FromMinutes(1);
            _timeBetweenLastEnqueueAndPreviousEnqueue = TimeSpan.FromMinutes(10);
            _timeBetweenCheckpoints = TimeSpan.FromMinutes(1);

            _howFarBehindForCheckpoint = 1500;
            _howFarBehindForPreviousEnqueue = 6000;

            _lastEnqueuedOffset = "12345";
            _lastEnqueuedTimeUtc = new DateTime(1975, 4, 1);
            _lastCheckpointTimeUtc = _lastEnqueuedTimeUtc.Subtract(timeBetweenLastEnqueueAndLastCheckpoint);
            _incomingByteRate = 99;
            _outgoingByteRate = 111;

            var mostRecentCheckpoint = new PartitionCheckpoint
            {
                SequenceNumber = _sequenceNumberOfMostRecentEvent - _howFarBehindForCheckpoint,
                LastCheckpointTimeUtc = _lastCheckpointTimeUtc
            };

            var monitor = new EventHubMonitor(
                new[] {PartitionId},
                partitionId => Task.FromResult(mostRecentCheckpoint),
                partitionId => Task.FromResult(CreatePartitionDescription(partitionId)),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));

            _snapshot = monitor
                .CaptureSnapshotAsync(PartitionId)
                .Result;
        }

        private PartitionDescription CreatePartitionDescription(string partitionId)
        {
            // We're using reflection to construct an instance of the this time.
            // It is part of the Event Hub (Service Bus) SDK, and we're not able 
            // manipulate it directly.
            // At runtime, the instance will be supplied by the SDK itself.
            var map = new Dictionary<string, object>
            {
                {"InternalEndSequenceNumber", _sequenceNumberOfMostRecentEvent},
                {"InternalLastEnqueuedTimeUtc", _lastEnqueuedTimeUtc},
                {"InternalLastEnqueuedOffset", _lastEnqueuedOffset },
                {"InternalIncomingBytesPerSecond", _incomingByteRate },
                {"InternalOutgoingBytesPerSecond", _outgoingByteRate }
            };

            var p = new PartitionDescription("myhub", partitionId);

            var type = typeof (PartitionDescription);
            foreach (var pair in map)
            {
                var property = type.GetProperty(pair.Key, BindingFlags.Instance | BindingFlags.NonPublic);
                property.SetValue(p, pair.Value);
            }

            return p;
        }

        [Fact]
        public void should_include_the_parition_id()
        {
            Assert.Equal(PartitionId, _snapshot.PartitionId);
        }

        [Fact]
        public void should_include_the_latest_sequence_number()
        {
            Assert.Equal(_sequenceNumberOfMostRecentEvent, _snapshot.EndSequenceNumber);
        }

        [Fact]
        public void should_include_the_latest_enqueued_time()
        {
            Assert.Equal(_lastEnqueuedTimeUtc, _snapshot.LastEnqueuedTimeUtc);
        }

        [Fact]
        public void should_include_the_latest_enqueued_offset()
        {
            Assert.Equal(_lastEnqueuedOffset, _snapshot.LastEnqueuedOffset);
        }

        [Fact]
        public void should_include_the_incoming_bytes_per_second()
        {
            Assert.Equal(_incomingByteRate, _snapshot.IncomingBytesPerSecond);
        }

        [Fact]
        public void should_include_the_outgoing_bytes_per_second()
        {
            Assert.Equal(_outgoingByteRate, _snapshot.OutgoingBytesPerSecond);
        }

        [Fact]
        public void should_calculate_the_current_number_of_unprocessed_events()
        {
            Assert.Equal(_howFarBehindForCheckpoint, _snapshot.UnprocessedEvents);
        }

        [Fact]
        public void should_include_the_end_sequence_number_of_the_last_checkpoint()
        {
            var sequenceNumberOfMostRecentCheckpoint = _sequenceNumberOfMostRecentEvent - _howFarBehindForCheckpoint;
            Assert.Equal(sequenceNumberOfMostRecentCheckpoint, _snapshot.LastCheckpointSequenceNumber);
        }

        [Fact]
        public void should_include_the_time_of_the_last_checkpoint()
        {
            Assert.Equal(_lastCheckpointTimeUtc, _snapshot.LastCheckpointTimeUtc);
        }
    }
}