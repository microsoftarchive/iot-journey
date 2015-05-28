// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.IoTJourney.ColdStorage.Logging;
using Microsoft.ServiceBus;
using Microsoft.WindowsAzure.Storage;

namespace Microsoft.Practices.IoTJourney.ColdStorage.ConsoleHost
{
    internal class Program
    {

        private static void Main(string[] args)
        {
            var observableEventListener = new ObservableEventListener();

            observableEventListener.EnableEvents(
              ColdStorageEventSource.Log, EventLevel.Informational);

            observableEventListener.LogToConsole();

            Tests.Common.ConsoleHost.WithOptions(new Dictionary<string, Func<CancellationToken, Task>>
            {
                { "Provision Resources", ProvisionResourcesAsync },
                { "Run Cold Storage Consumer", RunAsync }
            });
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

            Console.WriteLine("Confirming blob container: {0}", configuration.ContainerName);

            var storageClient = CloudStorageAccount
                .Parse(configuration.BlobWriterStorageAccount)
                .CreateCloudBlobClient();

            var container = storageClient.GetContainerReference(configuration.ContainerName);
            await container.CreateIfNotExistsAsync(token);
            Console.WriteLine("container confirmed");
        }

        private static async Task RunAsync(CancellationToken token)
        {
            var configuration = Configuration.GetCurrentConfiguration();
            ColdStorageCoordinator processor = null;
            
            processor =
                await ColdStorageCoordinator.CreateAsync("Console", configuration);

            Console.WriteLine("Running processor");

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            catch (TaskCanceledException) { /* expected cancellation */ }

            processor.Dispose();
        }
    }
}

