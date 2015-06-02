// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Microsoft.Practices.IoTJourney.ColdStorage.RollingBlobWriter;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Practices.IoTJourney.ColdStorage
{
    public class ColdStorageEventProcessorFactory : IEventProcessorFactory
    {
        private readonly Func<string, IBlobWriter> _blobWriterFactory = null;
        private readonly int _warningLevel;
        private readonly int _tripLevel;
        private readonly TimeSpan _stallInterval;
        private readonly TimeSpan _logCooldownInterval;
        private readonly string _eventHubName;

        public ColdStorageEventProcessorFactory(
            Func<string, IBlobWriter> blobWriterFactory,
            int warningLevel,
            int tripLevel,
            TimeSpan stallInterval,
            TimeSpan logCooldownInterval,
            string eventHubName)
        {

            Guard.ArgumentNotNull(blobWriterFactory, "blobWriterFactory");

            _blobWriterFactory = blobWriterFactory;
            _warningLevel = warningLevel;
            _tripLevel = tripLevel;
            _stallInterval = stallInterval;
            _logCooldownInterval = logCooldownInterval;
            _eventHubName = eventHubName;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            var processor = new ColdStorageProcessor(
                _blobWriterFactory,
                _warningLevel,
                _tripLevel,
                _stallInterval,
                _logCooldownInterval, 
                _eventHubName
            );
            return processor;
        }
    }
}
