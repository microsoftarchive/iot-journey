using Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost.Formatters;
using Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost.Sinks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var consumerGroupName = "cg-elasticsearch";

            var monitor = new PartitionMonitor
            (
                samplingRate: TimeSpan.FromSeconds(30),
                sessionTimeout: TimeSpan.FromMinutes(60),
                consumerGroupName: consumerGroupName,
                checkpointContainerName: "eventhub-iot"
            );

            var formatter = new CsvEventTextFormatter();
            var filename = string.Format("Data\\{0}-{1}.csv", consumerGroupName, DateTime.Now.ToString("yyyy-MM-dd-hh-mm"));
            var sink = new FlatFileSink(filename, formatter, true);

            var outputDir = Path.Combine(Environment.CurrentDirectory, "Data");
            if(!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            monitor.StartAsync().Wait();

            monitor.Stream.Subscribe(@event =>
            {
                sink.OnNext(@event);
                
                Console.WriteLine("Partition {0}", @event.PartitionId);
                Console.WriteLine("----------");
                Console.WriteLine("- TimeStamp: {0}", @event.TimeStamp);
                Console.WriteLine("- PreciseTimeStamp: {0}", @event.PreciseTimeStamp);
                Console.WriteLine("- IncomingEventsPerSecond: {0}", @event.IncomingEventsPerSecond);
                Console.WriteLine("- IncomingBytesPerSecond: {0}", @event.IncomingBytesPerSecond);
                Console.WriteLine("- OutgoingEventsPerSecond: {0}", @event.OutgoingEventsPerSecond);
                Console.WriteLine("- OutgoingBytesPerSecond: {0}", @event.OutgoingBytesPerSecond);
                Console.WriteLine("- UnprocessedEvents: {0}", @event.UnprocessedEvents);
                Console.WriteLine("");
            });

            Console.ReadKey();
        }
    }
}
