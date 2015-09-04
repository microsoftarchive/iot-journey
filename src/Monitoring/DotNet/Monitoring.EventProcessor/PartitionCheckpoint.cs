using System;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor
{
    public class PartitionCheckpoint
    {
        public string PartitionId { get; set; }
        public string Owner { get; set; }
        public string Token { get; set; }
        public long Epoch { get; set; }
        // NOTE: sometimes `Offset` contains an empty string in the JSON
        public string Offset { get; set; }
        public long SequenceNumber { get; set; }
        // `LastCheckpointTimeUtc` is not a part of the JSON, but set explicitly
        public DateTimeOffset LastCheckpointTimeUtc { get; set; } 
    }
}