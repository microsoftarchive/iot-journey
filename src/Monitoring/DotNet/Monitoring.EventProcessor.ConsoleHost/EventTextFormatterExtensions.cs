using Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost.Formatters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost
{
    /// <summary>
    /// Extensions for <see cref="IEventTextFormatter"/>.
    /// </summary>
    public static class EventTextFormatterExtensions
    {
        /// <summary>
        /// Formats the event as a string.
        /// </summary>
        /// <param name="snapshot">The partition snapshot to format.</param>
        /// <param name="formatter">The formatter to use.</param>
        /// <returns>A formatted snapshot.</returns>
        public static string WriteEvent(this IEventTextFormatter formatter, PartitionSnapshot snapshot)
        {
            Guard.ArgumentNotNull(formatter, "formatter");

            using (var writer = new StringWriter(CultureInfo.CurrentCulture))
            {
                formatter.WriteEvent(snapshot, writer);
                return writer.ToString();
            }
        }
    }
}
