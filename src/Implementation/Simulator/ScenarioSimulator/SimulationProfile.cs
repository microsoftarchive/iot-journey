// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.Logging;
using Microsoft.Practices.IoTJourney.ScenarioSimulator.Instrumentation;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator
{
    public class SimulationProfile
    {
        private readonly SimulatorConfiguration _simulatorConfiguration;

        // The instrumentation publisher is responsible for updating 
        // performance counters and sending related telemetry events.
        private readonly ISenderInstrumentationPublisher _instrumentationPublisher;

        private readonly string _hostName;

        private readonly int _devicesPerInstance;

        private readonly ISubject<int> _observableTotalCount = new Subject<int>();

        public SimulationProfile(
            string hostName,
            int instanceCount,
            ISenderInstrumentationPublisher instrumentationPublisher,
            SimulatorConfiguration simulatorConfiguration)
        {
            _hostName = hostName;
            _instrumentationPublisher = instrumentationPublisher;
            _simulatorConfiguration = simulatorConfiguration;

            _devicesPerInstance = simulatorConfiguration.NumberOfDevices / instanceCount;
        }

        public async Task RunSimulationAsync(string scenario, CancellationToken token)
        {
            ScenarioSimulatorEventSource.Log.SimulationStarted(_hostName, scenario);

            var produceEventsForScenario = SimulationScenarios.GetScenarioByName(scenario);

            var simulationTasks = new List<Task>();

            var warmup = _simulatorConfiguration.WarmupDuration;
            var warmupPerDevice = warmup.Ticks / _devicesPerInstance;

            var messagingFactories =
                Enumerable.Range(0, _simulatorConfiguration.SenderCountPerInstance)
                    .Select(i => MessagingFactory.CreateFromConnectionString(_simulatorConfiguration.EventHubConnectionString))
                    .ToArray();

            _observableTotalCount
                .Sum()
                .Subscribe(total => ScenarioSimulatorEventSource.Log.FinalEventCountForAllDevices(total));

            _observableTotalCount
                .Buffer(TimeSpan.FromMinutes(5))
                .Scan(0, (total, next) => total + next.Sum())
                .Subscribe(total => ScenarioSimulatorEventSource.Log.CurrentEventCountForAllDevices(total));

            try
            {
                for (int i = 0; i < _devicesPerInstance; i++)
                {
                    // Use the short form of the host or instance name to generate the vehicle ID
                    var deviceId = String.Format("{0}-{1}", ConfigurationHelper.InstanceName, i);

                    var eventSender = new EventSender(
                        messagingFactory: messagingFactories[i % messagingFactories.Length],
                        config: _simulatorConfiguration,
                        serializer: Serializer.ToJsonUTF8,
                        telemetryPublisher: _instrumentationPublisher
                    );

                    var deviceTask = SimulateDeviceAsync(
                        deviceId: deviceId,
                        produceEventsForScenario: produceEventsForScenario,
                        sendEventsAsync: eventSender.SendAsync,
                        waitBeforeStarting: TimeSpan.FromTicks(warmupPerDevice * i),
                        totalCount: _observableTotalCount,
                        token: token
                    );

                    simulationTasks.Add(deviceTask);
                }

                await Task.WhenAll(simulationTasks.ToArray());

                _observableTotalCount.OnCompleted();
            }
            finally
            {
                // cannot await on a finally block to do CloseAsync
                foreach (var factory in messagingFactories)
                {
                    factory.Close();
                }
            }

            ScenarioSimulatorEventSource.Log.SimulationEnded(_hostName);
        }

        private static async Task SimulateDeviceAsync(
            string deviceId,
            Func<EventEntry[]> produceEventsForScenario,
            Func<string, object, Task<bool>> sendEventsAsync,
            TimeSpan waitBeforeStarting,
            IObserver<int> totalCount,
            CancellationToken token)
        {
            ScenarioSimulatorEventSource.Log.WarmingUpFor(deviceId, waitBeforeStarting.Ticks);

            try
            {
                await Task.Delay(waitBeforeStarting, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            var messagingEntries = produceEventsForScenario();
            var device = new Device(deviceId, messagingEntries, sendEventsAsync);

            device.ObservableEventCount
                .Sum()
                .Subscribe(total => ScenarioSimulatorEventSource.Log.FinalEventCount(deviceId, total));

            device.ObservableEventCount
                .Subscribe(totalCount.OnNext);

            await device.RunSimulationAsync(token);
        }
    }
}