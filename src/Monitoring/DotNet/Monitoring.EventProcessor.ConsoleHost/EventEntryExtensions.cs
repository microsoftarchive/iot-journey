using Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost
{
    public static class EventEntryExtensions
    {
        /// <summary>
        /// Formats an <see cref="EventEntry"/> as a string using an <see cref="IEventTextFormatter"/>.
        /// </summary>
        /// <param name="entry">The entry to format.</param>
        /// <param name="formatter">The formatter to use.</param>
        /// <returns>A formatted entry, or <see langword="null"/> if an exception is thrown by the <paramref name="formatter"/>.</returns>
        public static string TryFormatAsString(this EventEntry entry, IEventTextFormatter formatter)
        {
            try
            {
                return formatter.WriteEvent(entry);
            }
            catch (Exception e)
            {
                //SemanticLoggingEventSource.Log.FormatEntryAsStringFailed(e.ToString());
            }

            return null;
        }
    }
}
