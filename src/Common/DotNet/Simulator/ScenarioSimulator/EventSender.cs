// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.Devices.Events;
using Microsoft.Practices.IoTJourney.Logging;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator
{
    public class EventSender : IEventSender
    {
        private readonly EventHubSender _eventHubSender;

        private readonly Func<object, byte[]> _serializer;

        public EventSender(
            Device device,
            SimulatorConfiguration config,
            Func<object, byte[]> serializer)
        {
            this._serializer = serializer;

            var connectionString = ServiceBusConnectionStringBuilder.CreateUsingSharedAccessSignature(
                                         device.Endpoint,
                                         device.EventHubName,
                                         device.Id,
                                         device.Token
                                   );

            _eventHubSender = EventHubSender.CreateFromConnectionString(connectionString);
        }

        public static string DetermineTypeFromEvent(object evt)
        {
            return evt.GetType().Name;
        }

        public async Task<bool> SendAsync(object evt)
        {
            try
            {
                var bytes = this._serializer(evt);

                using (var eventData = new EventData(bytes))
                {
                    var stopwatch = Stopwatch.StartNew();

                    await _eventHubSender.SendAsync(eventData);
                    stopwatch.Stop();

                    ScenarioSimulatorEventSource.Log.EventSent(stopwatch.ElapsedTicks);

                    return true;
                }
            }
            catch (ServerBusyException e)
            {
                ScenarioSimulatorEventSource.Log.ServiceThrottled(e);
            }
            catch (Exception e)
            {
                ScenarioSimulatorEventSource.Log.UnableToSend(e, evt.ToString());
            }

            return false;
        }
    }
}