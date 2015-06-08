// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.WarmStorage.Logging;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Practices.IoTJourney.WarmStorage.ElasticSearchWriter;

namespace Microsoft.Practices.IoTJourney.WarmStorage
{
    public class WarmStorageCoordinator
    {
        private EventProcessorHost _host;

        private WarmStorageCoordinator(EventProcessorHost host)
        {
            _host = host;
        }

        public static async Task<WarmStorageCoordinator> CreateAsync(string hostName, Configuration configuration)
        {
            WarmStorageEventSource.Log.InitializingEventHubListener(configuration.EventHubName, configuration.ConsumerGroupName);

            Func<string, IElasticSearchWriter> elasticSearchWriterFactory = partitionId => 
                new ElasticSearchWriter.ElasticSearchWriter(
                    configuration.ElasticSearchUrl,
                    configuration.ElasticSearchIndexPrefix,
                    configuration.ElasticSearchIndexType,
                    configuration.RetryCount
                );

            var ns = NamespaceManager.CreateFromConnectionString(configuration.EventHubConnectionString);
            try
            {
                await ns.GetConsumerGroupAsync(configuration.EventHubName, configuration.ConsumerGroupName);
            }
            catch (Exception e)
            {
                WarmStorageEventSource.Log.InvalidEventHubConsumerGroupName(e, configuration.EventHubName, configuration.ConsumerGroupName);
                throw;
            }

            WarmStorageEventSource.Log.ConsumerGroupFound(configuration.EventHubName, configuration.ConsumerGroupName);

            var eventHubId = ConfigurationHelper.GetEventHubName(ns.Address, configuration.EventHubName);

            var factory = new WarmStorageEventProcessorFactory(elasticSearchWriterFactory, eventHubId);

            var options = new EventProcessorOptions()
            {
                MaxBatchSize = configuration.MaxBatchSize,
                PrefetchCount = configuration.PreFetchCount,
                ReceiveTimeOut = configuration.ReceiveTimeout,
                InvokeProcessorAfterReceiveTimeout = true
            };

            options.ExceptionReceived += 
                (s, e) => WarmStorageEventSource.Log.ErrorProcessingMessage(e.Exception, e.Action);

            var host = new EventProcessorHost(
                hostName,
                consumerGroupName: configuration.ConsumerGroupName,
                eventHubPath: configuration.EventHubName,
                eventHubConnectionString: configuration.EventHubConnectionString,
                storageConnectionString: configuration.CheckpointStorageAccount);

            await host.RegisterEventProcessorFactoryAsync(factory, options);

            return new WarmStorageCoordinator(host);
        }

        public async Task TearDownAsync()
        {
            if (_host != null)
            {
                await _host.UnregisterEventProcessorAsync();
                _host = null;
            }
        }
    }
}
