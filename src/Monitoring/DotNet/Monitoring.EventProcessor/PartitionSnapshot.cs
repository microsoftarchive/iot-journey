// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor
{
    public class PartitionSnapshot
    {
        /// <summary>
        ///     Id of the partition.
        /// </summary>
        public string PartitionId { get; set; }

        /// <summary>
        ///     Timestamp of the last known checkpoint.
        /// </summary>
        public DateTimeOffset LastCheckpointTimeUtc { get; set; }

        /// <summary>
        ///     Timestamp of the last seen sequence number on the partition.
        /// </summary>
        public DateTimeOffset LastEnqueuedTimeUtc { get; set; }

        /// <summary>
        ///     The numbers of events that remain unprocessed for a given consumer group.
        /// </summary>
        public long UnprocessedEvents { get; set; }

        /// <summary>
        ///     The last seen sequence number on the partition. 
        ///     This reflects the identity of the most recent event data.
        /// </summary>
        public long EndSequenceNumber { get; set; }

        /// <summary>
        ///     Timestamp of when this snapshot was captured.
        /// </summary>
        public DateTimeOffset CapturedAtTimeUtc { get; set; }

        /// <summary>
        /// The current offset of the partition in bytes
        /// </summary>
        public string LastEnqueuedOffset { get; set; }

        /// <summary>
        /// The rate of ingress for the partition.
        /// </summary>
        public long IncomingBytesPerSecond { get; set; }

        /// <summary>
        /// The rate of egress for the parition.
        /// This does not necessarily reflect the state of
        /// the consumer group.
        /// </summary>
        public long OutgoingBytesPerSecond { get; set; }
    }
}