// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Practices.IoTJourney.WarmStorage.ElasticSearchWriter
{
    public interface IElasticSearchWriter
    {
        Task<bool> WriteAsync(IReadOnlyCollection<EventData> events, CancellationToken cancellationToken);
    }
}
