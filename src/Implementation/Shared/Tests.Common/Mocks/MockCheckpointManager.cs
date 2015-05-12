// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Practices.IoTJourney.Tests.Common.Mocks
{
    public class MockCheckpointManager : ICheckpointManager
    {
        private readonly Func<Task> _checkpointAsync;

        public MockCheckpointManager(Func<Task> checkpointAsync)
        {
            _checkpointAsync = checkpointAsync;
        }

        public Task CheckpointAsync(Lease lease, string offset, long sequenceNumber)
        {
            return _checkpointAsync();
        }
    }
}