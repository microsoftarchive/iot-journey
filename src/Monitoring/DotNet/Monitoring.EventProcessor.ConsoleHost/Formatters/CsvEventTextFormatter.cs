// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost.Formatters
{
    public class CsvEventTextFormatter : IEventTextFormatter
    {
        public void WriteEvent(PartitionSnapshot snapshot, TextWriter writer)
        {
            Guard.ArgumentNotNull(snapshot, "PartitionSnapshot");
            Guard.ArgumentNotNull(writer, "writer");

            writer.WriteLine("{0},{1},{2}",
                snapshot.PartitionId,
                snapshot.CapturedAtTimeUtc,
                snapshot.UnprocessedEvents);
        }
    }
}