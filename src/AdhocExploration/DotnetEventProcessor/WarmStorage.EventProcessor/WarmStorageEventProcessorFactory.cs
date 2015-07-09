using Microsoft.Practices.IoTJourney.WarmStorage.ElasticSearchWriter;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            var processor = new WarmStorageProcessor(_elasticSearchWriterFactory, _eventHubName);
            return processor;
        }
    }
}
