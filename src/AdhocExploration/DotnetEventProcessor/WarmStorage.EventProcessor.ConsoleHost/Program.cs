// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.IoTJourney;
using Microsoft.Practices.IoTJourney.WarmStorage.Logging;
using Microsoft.ServiceBus;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Practices.IoTJourney.WarmStorage;
using CommonConsoleHost = Microsoft.Practices.IoTJourney.Tests.Common.ConsoleHost;


namespace WarmStorage.ConsoleHost
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            var observableEventListener = new ObservableEventListener();

            observableEventListener.EnableEvents(
              WarmStorageEventSource.Log, EventLevel.Informational);

            observableEventListener.LogToConsole();

            await CommonConsoleHost.RunWithOptionsAsync(new Dictionary<string, Func<CancellationToken, Task>>
            {
                { "Provision Resources", ProvisionResourcesAsync },
                { "Run Warm Storage Consumer", RunAsync }
            }).ConfigureAwait(false);
        }

        private static async Task ProvisionResourcesAsync(CancellationToken token)
        {
            var configuration = Configuration.GetCurrentConfiguration();

            var nsm = NamespaceManager.CreateFromConnectionString(configuration.EventHubConnectionString);

            Console.WriteLine("EventHub name/path: {0}", configuration.EventHubName);

            Console.WriteLine("Confirming consumer group: {0}", configuration.ConsumerGroupName);
            await nsm.CreateConsumerGroupIfNotExistsAsync(
                    eventHubPath: configuration.EventHubName,
                    name: configuration.ConsumerGroupName);

            Console.WriteLine("Consumer group confirmed");

            //TODO: we are asuming Elastic Search is installed.
        }

        private static async Task RunAsync(CancellationToken token)
        {
            var configuration = Configuration.GetCurrentConfiguration();
            WarmStorageCoordinator processor = null;

            try
            {
                //TODO: using fixed names causes name collision when more than one instance is created within the processor host.
                //Consider changing to use Guid instead.
                processor = await WarmStorageCoordinator.CreateAsync("Console", configuration);

                Console.WriteLine("Running processor");

                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            catch (Exception) { }

            if (processor != null)
            {
                await processor.TearDownAsync();
            }
        }
    }
}
