// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost.Formatters;
using Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost.Sinks;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // This is mean to be an example of how you can
            // use the monitor. It can be hosted in a console app,
            // web app, or whatever works for your situation.
            var configuration = Configuration.GetCurrentConfiguration();
            var monitor = EventHubMonitorFactory.CreateAsync(configuration).Result;

            // We want to write out the data we collect to a
            // flat file, formatted as CSV. 
            // We can import this file into some tool, 
            // e.g. Excel or PowerBI, for visualization..
            var flatFileSink = CreateFlatFileSink(configuration.ConsumerGroupName);

            // The monitor allows for multiple subscribers.
            // First, we'll subscribe the flat file sink.
            monitor.Subscribe(flatFileSink);

            // Now, we'll have a second subscription that 
            // simply writes the data to the console.
            monitor.Subscribe(snapshot =>
            {
                Console.WriteLine("Partition {0}", snapshot.PartitionId);
                Console.WriteLine("----------");
                Console.WriteLine("- CapturedAtTimeUtc: {0}", snapshot.CapturedAtTimeUtc);
                Console.WriteLine("- LastCheckpointTimeUtc: {0}", snapshot.LastCheckpointTimeUtc);
                Console.WriteLine("- LastEnqueuedTimeUtc: {0:}", snapshot.LastEnqueuedTimeUtc);
                Console.WriteLine("- UnprocessedEvents: {0}", snapshot.UnprocessedEvents);
                Console.WriteLine();
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

        private static FlatFileSink CreateFlatFileSink(string consumerGroupName)
        {
            var formatter = new CsvEventTextFormatter();
            var filename = string.Format(
                CultureInfo.InvariantCulture,
                "Data\\{0}-{1}.csv",
                consumerGroupName,
                DateTime.Now.ToString("yyyy-MM-dd-hh-mm"));

            var sink = new FlatFileSink(filename, formatter, true);

            var outputDir = Path.Combine(Environment.CurrentDirectory, "Data");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            return sink;
        }
    }
}