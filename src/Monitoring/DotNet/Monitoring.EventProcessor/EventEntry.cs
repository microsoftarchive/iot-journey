using System;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor
{
    public class EventEntry
    {
        /// <summary>
        ///     Timestamp of the sample. All partitions collected in the same Sample will have this same Timestamp.
        /// </summary>
        public DateTimeOffset TimeStamp { get; set; }

        /// <summary>
        ///     Timestamp of the moment when the partition data was retrieved.
        /// </summary>
        public DateTimeOffset PreciseTimeStamp { get; set; }

        /// <summary>
        ///     Id of the partition.
        /// </summary>
        public int PartitionId { get; set; }

        /// <summary>
        ///     Offset in bytes.
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        ///     Sequence number.
        /// </summary>
        public long SequenceNumber { get; set; }

        /// <summary>
        ///     The numbers of events that remain unprocessed for a given consumer group.
        /// </summary>
        public long UnprocessedEvents { get; set; }

        public long IncomingBytesPerSecond { get; set; }

        public long OutgoingBytesPerSecond { get; set; }

        public long EndSequenceNumber { get; set; }

        public int IncomingEventsPerSecond { get; set; }

        public int OutgoingEventsPerSecond { get; set; }
    }
}