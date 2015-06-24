using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.WarmStorage.ElasticSearchWriter
{
    public interface IElasticSearchWriter
    {
        Task<bool> WriteAsync(IReadOnlyCollection<EventData> events, CancellationToken cancellationToken);
    }
}
