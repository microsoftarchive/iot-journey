using System.IO;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost.Formatters
{
    public class CsvEventTextFormatter : IEventTextFormatter
    {
        public void WriteEvent(PartitionSnapshot snapshot, TextWriter writer)
        {
            Guard.ArgumentNotNull(snapshot, "PartitionSnapshot");
            Guard.ArgumentNotNull(writer, "writer");

            writer.WriteLine("{0},{1},{2},{3}",
                snapshot.PartitionId,
                snapshot.LastCheckpointTimeUtc,
                snapshot.LastEnqueuedTimeUtc,
                snapshot.UnprocessedEvents);
        }
    }
}