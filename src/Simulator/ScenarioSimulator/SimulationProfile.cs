// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.Logging;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator
{
    public class SimulationProfile
    {
        private readonly SimulatorConfiguration _simulatorConfiguration;

        private readonly string _hostName;

        private readonly ISubject<int> _eventsSentCount = new Subject<int>();

        private readonly IList<Device> _devices = new List<Device>();

        public SimulationProfile(
            string hostName,
            SimulatorConfiguration simulatorConfiguration)
        {
            Guard.ArgumentNotNullOrEmpty(hostName, "hostName");
            Guard.ArgumentNotNull(simulatorConfiguration, "simulatorConfiguration");

            _hostName = hostName;
            _simulatorConfiguration = simulatorConfiguration;
        }

        public void ProvisionDevices(bool force)
        {
            //ScenarioSimulatorEventSource.Log.ProvisionDevicesSatarted();

            if (_devices.Any() && !force)
            {
                throw new InvalidOperationException("Devices already provisioned. Use force option to reprovision.");
            }

            _devices.Clear();

            for (int i = 0; i < _simulatorConfiguration.NumberOfDevices; i++)
            {
                // Use the short form of the host or instance name to generate the device id.
                var deviceId = String.Format("{0}-{1}", ConfigurationHelper.InstanceName, i);

                var endpoint = ServiceBusEnvironment.CreateServiceUri("sb", _simulatorConfiguration.EventHubNamespace, string.Empty);
                var eventHubName = _simulatorConfiguration.EventHubName;

                // Generate token for the device.
                string deviceToken = SharedAccessSignatureTokenProvider.GetPublisherSharedAccessSignature
                (
                    endpoint,
                    eventHubName,
                    deviceId,
                    _simulatorConfiguration.EventHubSasKeyName,
                    _simulatorConfiguration.EventHubPrimaryKey,
                    TimeSpan.FromDays(_simulatorConfiguration.EventHubTokenLifetimeDays)
                );

                _devices.Add(new Device(deviceId, endpoint, eventHubName, i) { Token = deviceToken });
            }
        }

        private static void ObserveScenarioOuput(IObservable<int> count)
        {
            count
                .Sum()
                .Subscribe(total => ScenarioSimulatorEventSource.Log.FinalEventCountForAllDevices(total));

            count
                .Buffer(TimeSpan.FromMinutes(5))
                .Scan(0, (total, next) => total + next.Sum())
                .Subscribe(total => ScenarioSimulatorEventSource.Log.CurrentEventCountForAllDevices(total));

            count
                .Buffer(TimeSpan.FromMinutes(0.1))
                .TimeInterval()
                .Select(x => x.Value.Sum() / x.Interval.TotalSeconds)
                .Subscribe(rate => ScenarioSimulatorEventSource.Log.CurrentEventsPerSecond(String.Format("{0:0.00} per second", rate)));
        }

        public async Task RunSimulationAsync(string scenario, CancellationToken token)
        {
            //TODO: we need to find a friendlier way to show this.
            if (!_devices.Any())
            {
                throw new InvalidOperationException("No devices found. Please execute device provisioning first.");
            }

            ScenarioSimulatorEventSource.Log.SimulationStarted(_hostName, scenario);

            var produceEventsForScenario = SimulationScenarios.GetScenarioByName(scenario);

            var simulationTasks = new List<Task>();

            var warmup = _simulatorConfiguration.WarmUpDuration;
            var warmupPerDevice = warmup.Ticks / _devices.Count;

            ObserveScenarioOuput(_eventsSentCount);

            foreach (var device in _devices)
            {
                var eventSender = new EventSender(
                    device: device,
                    config: _simulatorConfiguration,
                    serializer: Serializer.ToJsonUTF8
                );

                var deviceTask = SimulateDeviceAsync(
                    device: device,
                    produceEventsForScenario: produceEventsForScenario,
                    sendEventsAsync: eventSender.SendAsync,
                    waitBeforeStarting: TimeSpan.FromTicks(warmupPerDevice * device.StartupOrder),
                    totalCount: _eventsSentCount,
                    token: token
                );

                simulationTasks.Add(deviceTask);
            }

            await Task.WhenAll(simulationTasks.ToArray()).ConfigureAwait(false);

            _eventsSentCount.OnCompleted();

            ScenarioSimulatorEventSource.Log.SimulationEnded(_hostName);
        }

        private static async Task SimulateDeviceAsync(
            Device device,
            Func<EventEntry[]> produceEventsForScenario,
            Func<object, Task<bool>> sendEventsAsync,
            TimeSpan waitBeforeStarting,
            IObserver<int> totalCount,
            CancellationToken token)
        {
            ScenarioSimulatorEventSource.Log.WarmingUpFor(device.Id, waitBeforeStarting.Ticks);

            try
            {
                await Task.Delay(waitBeforeStarting, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            var messagingEntries = produceEventsForScenario();

            device.ObservableEventCount
                .Sum()
                .Subscribe(total => ScenarioSimulatorEventSource.Log.FinalEventCount(device.Id, total));

            device.ObservableEventCount
                .Subscribe(totalCount.OnNext);

            await device.RunSimulationAsync(messagingEntries, sendEventsAsync, token).ConfigureAwait(false);
        }
    }
}