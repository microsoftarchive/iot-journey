using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
