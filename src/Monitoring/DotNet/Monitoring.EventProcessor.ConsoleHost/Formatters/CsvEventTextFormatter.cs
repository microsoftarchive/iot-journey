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
        public void WriteEvent(EventEntry eventEntry, TextWriter writer)
        {
            Guard.ArgumentNotNull(eventEntry, "eventEntry");
            Guard.ArgumentNotNull(writer, "writer");

            writer.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                            eventEntry.PartitionId,
                            eventEntry.LastCheckpointTimeUtc,
                            eventEntry.LastEnqueuedTimeUtc,
                            eventEntry.IncomingEventsPerSecond,
                            eventEntry.IncomingBytesPerSecond,
                            eventEntry.OutgoingEventsPerSecond,
                            eventEntry.OutgoingBytesPerSecond,
                            eventEntry.UnprocessedEvents);
        }
    }
}
