using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor
{
    public static class EventHubMonitorFactory
    {
        public static Task<PartitionMonitor> CreateAsync(Configuration configuration)
        {
            return CreateAsync(configuration,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(30));
        }

        public static async Task<PartitionMonitor> CreateAsync(
            Configuration configuration,
            TimeSpan pauseBetweenParitions,
            TimeSpan pauseAfterAllPartitions
            )
        {
            var nsm = CreateNamespaceManager(configuration, pauseBetweenParitions);

            // create a partially applied function so that 
            // we'll only need to pass the partition id
            Func<string, Task<PartitionDescription>> getEventHubPartitionAsync =
                partitionId => nsm.GetEventHubPartitionAsync(configuration.EventHubName, configuration.ConsumerGroupName, partitionId);

            var eventhub = await nsm.GetEventHubAsync(configuration.EventHubName).ConfigureAwait(false);

            // construct reference to the blob container
            var storageAccount = CloudStorageAccount.Parse(configuration.CheckpointStorageAccount);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var checkpointContainer = blobClient.GetContainerReference(configuration.EventHubName);

            // this manages the complexity of getting the 
            // latest checkpoint for each parition
            var checkpoints = new PartitionCheckpointManager(
                configuration.ConsumerGroupName,
                eventhub.PartitionIds,
                checkpointContainer
                );

            return new PartitionMonitor(
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