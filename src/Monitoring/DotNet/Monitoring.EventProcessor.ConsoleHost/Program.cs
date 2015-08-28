using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
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
            var monitor = new PartitionMonitor
            (
                samplingRate: TimeSpan.FromSeconds(30),
                sessionTimeout: TimeSpan.FromMinutes(5),
                consumerGroupName: "cg-elasticsearch",
                checkpointContainerName: "eventhub-iot"
            );

            monitor.StartAsync().Wait();

            monitor.Stream.Subscribe(pi =>
            {
                Console.WriteLine("Partition {0}", pi.PartitionId);
                Console.WriteLine("----------");
                Console.WriteLine("- TimeStamp: {0}", pi.TimeStamp);
                Console.WriteLine("- PreciseTimeStamp: {0}", pi.PreciseTimeStamp);
                Console.WriteLine("- IncomingEventsPerSecond: {0}", pi.IncomingEventsPerSecond);
                Console.WriteLine("- IncomingBytesPerSecond: {0}", pi.IncomingBytesPerSecond);
                Console.WriteLine("- OutgoingEventsPerSecond: {0}", pi.OutgoingEventsPerSecond);
                Console.WriteLine("- OutgoingBytesPerSecond: {0}", pi.OutgoingBytesPerSecond);
                Console.WriteLine("- UnprocessedEvents: {0}", pi.UnprocessedEvents);
                Console.WriteLine("");
            });

            Console.ReadKey();
        }
    }
}
