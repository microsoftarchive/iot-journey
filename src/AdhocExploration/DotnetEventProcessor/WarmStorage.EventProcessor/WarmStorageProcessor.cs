// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.Devices.Events;
using Microsoft.Practices.IoTJourney.WarmStorage.EventProcessor.ElasticSearchWriter;
using Microsoft.Practices.IoTJourney.WarmStorage.EventProcessor.Logging;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace Microsoft.Practices.IoTJourney.WarmStorage.EventProcessor
{
    public class WarmStorageProcessor : IEventProcessor
    {
        private readonly Func<string, IElasticSearchWriter> _elasticSearchWriterFactory;
        private IElasticSearchWriter _elasticSearchWriter;
        private readonly string _eventHubName;
        private readonly CancellationToken _token = CancellationToken.None;
        private readonly IBuildingLookupService _buildingLookupService;

        public WarmStorageProcessor(
            Func<string, IElasticSearchWriter> elasticSearchWriterFactory,
            string eventHubName, IBuildingLookupService buildingLookupService)
        {
            Guard.ArgumentNotNull(elasticSearchWriterFactory, "elasticSearchWriterFactory");
            Guard.ArgumentNotNullOrEmpty(eventHubName, "eventHubName");

            _elasticSearchWriterFactory = elasticSearchWriterFactory;
            _eventHubName = eventHubName;
            _buildingLookupService = buildingLookupService;
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

            foreach (var eventData in events)
            {
                var updateTemperatureEvent = JsonConvert.DeserializeObject<UpdateTemperatureEvent>(Encoding.UTF8.GetString(eventData.GetBytes()));
                eventData.Properties["BuildingId"] = _buildingLookupService.GetBuildingId(updateTemperatureEvent.DeviceId);
            }

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
