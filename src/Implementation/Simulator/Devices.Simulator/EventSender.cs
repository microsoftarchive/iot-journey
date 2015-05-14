// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.Devices.Events;
using Microsoft.Practices.IoTJourney.Devices.Simulator.Instrumentation;
using Microsoft.Practices.IoTJourney.Logging;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Practices.IoTJourney.Devices.Simulator
{
    public class EventSender : IEventSender
    {
        private readonly EventHubClient _eventHubClient;

        private readonly ISenderInstrumentationPublisher _instrumentationTelemetryPublisher;

        private readonly Func<object, byte[]> _serializer;

        public EventSender(
            MessagingFactory messagingFactory,
            SimulatorConfiguration config,
            Func<object, byte[]> serializer,
            ISenderInstrumentationPublisher telemetryPublisher)
        {
            this._serializer = serializer;
            this._instrumentationTelemetryPublisher = telemetryPublisher;

            this._eventHubClient = messagingFactory.CreateEventHubClient(config.EventHubPath);
        }

        public static Tuple<string, int> DetermineTypeFromEvent(object evt)
        {
            // For the purposes of this simulation, we are defaulting
            // all type version numbers to 1.
            var type = evt.GetType().Name;
            return new Tuple<string, int>(type, 1);
        }

        public async Task<bool> SendAsync(string partitionKey, object evt)
        {
            try
            {
                var bytes = this._serializer(evt);

                using (var eventData = new EventData(bytes) { PartitionKey = partitionKey })
                {
                    var registration = DetermineTypeFromEvent(evt);
                    eventData.Properties[EventDataPropertyKeys.EventType] = registration.Item1;
                    eventData.Properties[EventDataPropertyKeys.EventTypeVersion] = registration.Item2;
                    eventData.Properties[EventDataPropertyKeys.DeviceId] = partitionKey;

                    var stopwatch = Stopwatch.StartNew();

                    this._instrumentationTelemetryPublisher.EventSendRequested();

                    await this._eventHubClient.SendAsync(eventData);
                    stopwatch.Stop();

                    this._instrumentationTelemetryPublisher.EventSendCompleted(
                        bytes.Length,
                        stopwatch.Elapsed);

                    ScenarioSimulatorEventSource.Log.EventSent(stopwatch.ElapsedTicks, partitionKey);

                    return true;
                }
            }
            catch (ServerBusyException e)
            {
                ScenarioSimulatorEventSource.Log.ServiceThrottled(e, partitionKey);
            }
            catch (Exception e)
            {
                ScenarioSimulatorEventSource.Log.UnableToSend(e, partitionKey, evt.ToString());
            }

            return false;
        }
    }
}