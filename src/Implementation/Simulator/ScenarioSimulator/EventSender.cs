// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.Devices.Events;
using Microsoft.Practices.IoTJourney.Logging;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator
{
    public class EventSender : IEventSender
    {
        private readonly EventHubClient _eventHubClient;

        private readonly Func<object, byte[]> _serializer;

        public EventSender(
            MessagingFactory messagingFactory,
            SimulatorConfiguration config,
            Func<object, byte[]> serializer)
        {
            this._serializer = serializer;

            this._eventHubClient = messagingFactory.CreateEventHubClient(config.EventHubPath);
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

                    await this._eventHubClient.SendAsync(eventData);
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