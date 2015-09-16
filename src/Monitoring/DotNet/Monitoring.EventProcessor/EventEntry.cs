using System;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor
{
    public class EventEntry
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
        ///     Offset in bytes.
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        ///     The numbers of events that remain unprocessed for a given consumer group.
        /// </summary>
        public long UnprocessedEvents { get; set; }

        public long EndSequenceNumber { get; set; }

        public bool IsStale { get; set; }


        public DateTimeOffset RecordedAtTimeUtc { get; set; }
    }
}