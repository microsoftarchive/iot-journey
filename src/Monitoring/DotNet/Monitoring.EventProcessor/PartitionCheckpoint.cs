using System;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor
{
    public class PartitionCheckpoint
    {
        public string PartitionId { get; set; }
        public string Owner { get; set; }
        public string Token { get; set; }
        public long Epoch { get; set; }

        // NOTE: `Offset` is a string instead of a long.
        // Sometimes the JSON contains an empty string for Offset.
        public string Offset { get; set; }
        public long SequenceNumber { get; set; }

        // `LastCheckpointTimeUtc` is not a part of the JSON,
        // it is explicitly set based on the meta data of the 
        // checkpoint blob.
        public DateTimeOffset LastCheckpointTimeUtc { get; set; }
    }
}