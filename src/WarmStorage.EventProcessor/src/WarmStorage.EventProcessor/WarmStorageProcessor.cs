using Microsoft.Practices.IoTJourney.WarmStorage.ElasticSearchWriter;
using Microsoft.Practices.IoTJourney.WarmStorage.Logging;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.WarmStorage
{
    public class WarmStorageProcessor : IEventProcessor
    {
        private readonly Func<string, IElasticSearchWriter> _elasticSearchWriterFactory;
        private IElasticSearchWriter _elasticSearchWriter;
        private readonly string _eventHubName;
        private readonly CancellationToken _token = CancellationToken.None;

        public WarmStorageProcessor(
            Func<string, IElasticSearchWriter> elasticSearchWriterFactory,
            string eventHubName)
        {
            Guard.ArgumentNotNull(elasticSearchWriterFactory, "elasticSearchWriterFactory");
            Guard.ArgumentNotNullOrEmpty(eventHubName, "eventHubName");

            _elasticSearchWriterFactory = elasticSearchWriterFactory;
            _eventHubName = eventHubName;
        }

        private const string ProcessorName = "elasticsearchwriter";

        public Task OpenAsync(PartitionContext context)
        {
            WarmStorageEventSource.Log.LeaseObtained(ProcessorName, _eventHubName, context.Lease.PartitionId);

            _elasticSearchWriter = _elasticSearchWriterFactory(context.Lease.PartitionId);

            return Task.FromResult(false);
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            return Task.FromResult(false);
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> events)
        {
            // Workaround for event hub sending null on timeout
            events = events ?? Enumerable.Empty<EventData>();

            if(!await _elasticSearchWriter.WriteAsync(events.ToList(), _token).ConfigureAwait(false))
            {
                return;
            }

            try
            {
                EventData checkpointEventData = events.LastOrDefault();

                await context.CheckpointAsync(checkpointEventData);

                WarmStorageEventSource.Log.CheckpointCompleted(ProcessorName, _eventHubName, context.Lease.PartitionId, checkpointEventData.Offset);
            }
            catch (Exception ex)
            {
                if (!(ex is StorageException || ex is LeaseLostException))
                {
                    throw;
                }

                WarmStorageEventSource.Log.UnableToCheckpoint(ex, ProcessorName, _eventHubName, context.Lease.PartitionId);
            }
        }
    }
}
