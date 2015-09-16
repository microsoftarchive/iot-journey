// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor
{
    public static class EventHubMonitorFactory
    {
        public static Task<EventHubMonitor> CreateAsync(Configuration configuration)
        {
            return CreateAsync(configuration,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(30));
        }

        public static async Task<EventHubMonitor> CreateAsync(
            Configuration configuration,
            TimeSpan pauseBetweenParitions,
            TimeSpan pauseAfterAllPartitions
            )
        {
            var nsm = CreateNamespaceManager(configuration, pauseBetweenParitions);

            // We create a partially applied function so that 
            // we'll only need to pass the partition id later.
            Func<string, Task<PartitionDescription>> getEventHubPartitionAsync =
                partitionId => nsm.GetEventHubPartitionAsync(configuration.EventHubName, configuration.ConsumerGroupName, partitionId);

            var eventhub = await nsm.GetEventHubAsync(configuration.EventHubName).ConfigureAwait(false);

            // Construct a reference to the blob container.
            var storageAccount = CloudStorageAccount.Parse(configuration.CheckpointStorageAccount);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var checkpointContainer = blobClient.GetContainerReference(configuration.EventHubName);

            // The `PartitionCheckpointManager` manages the complexity 
            // of getting the latest checkpoint for each parition.
            var checkpoints = new PartitionCheckpointManager(
                configuration.ConsumerGroupName,
                eventhub.PartitionIds,
                checkpointContainer
                );

            return new EventHubMonitor(
                eventhub.PartitionIds,
                checkpoints.GetLastCheckpointAsync,
                getEventHubPartitionAsync,
                pauseBetweenParitions,
                pauseAfterAllPartitions);
        }

        private static NamespaceManager CreateNamespaceManager(Configuration configuration, TimeSpan timeout)
        {
            var endpoint = ServiceBusEnvironment.CreateServiceUri("sb", configuration.EventHubNamespace, string.Empty);
            var connectionString = ServiceBusConnectionStringBuilder.CreateUsingSharedAccessKey(endpoint,
                configuration.EventHubSasKeyName,
                configuration.EventHubSasKey);

            var nsm = NamespaceManager.CreateFromConnectionString(connectionString);
            nsm.Settings.OperationTimeout = timeout;
            return nsm;
        }
    }
}