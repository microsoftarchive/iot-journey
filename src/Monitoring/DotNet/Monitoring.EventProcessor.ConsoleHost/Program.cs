// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost.Formatters;
using Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost.Sinks;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var configuration = Configuration.GetCurrentConfiguration();
            var monitor = EventHubMonitorFactory.CreateAsync(configuration).Result;

            var formatter = new CsvEventTextFormatter();
            var filename = string.Format(
                "Data\\{0}-{1}.csv", configuration.ConsumerGroupName,
                DateTime.Now.ToString("yyyy-MM-dd-hh-mm"));

            var sink = new FlatFileSink(filename, formatter, true);

            var outputDir = Path.Combine(Environment.CurrentDirectory, "Data");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            monitor.Subscribe(sink);
            monitor.Subscribe(snapshot =>
            {
                Console.WriteLine("Partition {0}", snapshot.PartitionId);
                Console.WriteLine("----------");
                Console.WriteLine("- CapturedAtTimeUtc: {0}", snapshot.CapturedAtTimeUtc);
                Console.WriteLine("- LastCheckpointTimeUtc: {0}", snapshot.LastCheckpointTimeUtc);
                Console.WriteLine("- LastEnqueuedTimeUtc: {0:}", snapshot.LastEnqueuedTimeUtc);
                Console.WriteLine("- UnprocessedEvents: {0}", snapshot.UnprocessedEvents);
                Console.WriteLine("");
            },
            e =>
            {
                Console.Error.WriteLine(e);
            },
            () =>
            {
                Console.WriteLine("Done");
            });

            Console.ReadKey();
        }
    }
}