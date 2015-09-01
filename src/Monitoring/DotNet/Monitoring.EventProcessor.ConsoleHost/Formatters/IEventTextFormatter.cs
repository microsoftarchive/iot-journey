using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost.Formatters
{
    public interface IEventTextFormatter
    {
        /// <summary>
        /// Writes the event.
        /// </summary>
        /// <param name="eventEntry">The event entry.</param>
        /// <param name="writer">The writer.</param>
        void WriteEvent(EventEntry eventEntry, TextWriter writer);
    }
}
