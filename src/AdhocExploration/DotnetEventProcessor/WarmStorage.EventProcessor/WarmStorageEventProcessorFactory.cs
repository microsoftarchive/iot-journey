// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Practices.IoTJourney.WarmStorage.ElasticSearchWriter;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Practices.IoTJourney.WarmStorage
{
    public class WarmStorageEventProcessorFactory : IEventProcessorFactory
    {
        private readonly Func<string, IElasticSearchWriter> _elasticSearchWriterFactory = null;
        private readonly string _eventHubName;
        private readonly IBuildingLookupService _buildingLookupService;

        public WarmStorageEventProcessorFactory(
            Func<string, IElasticSearchWriter> elasticSearchWriterFactory,
            string eventHubName,
            IBuildingLookupService buildingLookupService)
        {

            Guard.ArgumentNotNull(elasticSearchWriterFactory, "elasticSearchWriterFactory");
            Guard.ArgumentNotNullOrEmpty(eventHubName, "eventHubName");
            Guard.ArgumentNotNull(buildingLookupService, "buildingLookupService");

            _elasticSearchWriterFactory = elasticSearchWriterFactory;
            _eventHubName = eventHubName;
            _buildingLookupService = buildingLookupService;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            var processor = new WarmStorageProcessor(_elasticSearchWriterFactory, _eventHubName, _buildingLookupService);
            return processor;
        }
    }
}
