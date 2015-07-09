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

        public WarmStorageEventProcessorFactory(
            Func<string, IElasticSearchWriter> elasticSearchWriterFactory,
            string eventHubName)
        {

            Guard.ArgumentNotNull(elasticSearchWriterFactory, "elasticSearchWriterFactory");
            Guard.ArgumentNotNullOrEmpty(eventHubName, "eventHubName");

            _elasticSearchWriterFactory = elasticSearchWriterFactory;
            _eventHubName = eventHubName;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            var processor = new WarmStorageProcessor(_elasticSearchWriterFactory, _eventHubName, new BuildingLookupService());
            return processor;
        }
    }
}
