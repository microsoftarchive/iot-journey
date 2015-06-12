// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator
{
    public class SimulatorConfiguration
    {
        public string Scenario { get; set; }

        public int NumberOfDevices { get; set; }

        public string EventHubConnectionString { get; set; }

        public string EventHubPath { get; set; }

        public int SenderCountPerInstance { get; set; }

        public TimeSpan WarmupDuration { get; set; }

        public static SimulatorConfiguration GetCurrentConfiguration()
        {
            return new SimulatorConfiguration
            {
                EventHubConnectionString = ConfigurationHelper.GetConfigValue<string>("Simulator.EventHubConnectionString"),
                EventHubPath = ConfigurationHelper.GetConfigValue<string>("Simulator.EventHubPath"),
                NumberOfDevices = ConfigurationHelper.GetConfigValue<int>("Simulator.NumberOfDevices"),
                SenderCountPerInstance = ConfigurationHelper.GetConfigValue("Simulator.SenderCountPerInstance", 5),
                WarmupDuration = ConfigurationHelper.GetConfigValue("Simulator.WarmupDuration", TimeSpan.FromSeconds(30)),
                Scenario = ConfigurationHelper.GetConfigValue<string>("Simulator.Scenario", String.Empty)
            };
        }

        public override string ToString()
        {
            return String.Format(
                "Simulation SimulatorConfiguration; device count = {0} event hub name = {1}",
                NumberOfDevices,
                EventHubPath);
        }
    }
}