// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator
{
    public class SimulatorConfiguration
    {
        public string Scenario { get; set; }

        public int NumberOfDevices { get; set; }

        public string EventHubNamespace { get; set; }

        public string EventHubSasKeyName { get; set; }

        public string EventHubPrimaryKey { get; set; }

        public int EventHubTokenLifetimeDays { get; set; }

        public string EventHubConnectionString { get; set; }

        public string EventHubPath { get; set; }

        public TimeSpan WarmupDuration { get; set; }

        public EventLevel EventLevel { get; set; }

        public static SimulatorConfiguration GetCurrentConfiguration()
        {
            return new SimulatorConfiguration
            {
                EventHubNamespace = ConfigurationHelper.GetConfigValue<string>("Simulator.EventHubNamespace"),
                EventHubSasKeyName = ConfigurationHelper.GetConfigValue<string>("Simulator.EventHubSasKeyName"),
                EventHubPrimaryKey = ConfigurationHelper.GetConfigValue<string>("Simulator.EventHubPrimaryKey"),
                EventHubTokenLifetimeDays = ConfigurationHelper.GetConfigValue<int>("Simulator.EventHubTokenLifetimeDays", 7),
                EventHubConnectionString = ConfigurationHelper.GetConfigValue<string>("Simulator.EventHubConnectionString"),
                EventHubPath = ConfigurationHelper.GetConfigValue<string>("Simulator.EventHubPath"),
                NumberOfDevices = ConfigurationHelper.GetConfigValue<int>("Simulator.NumberOfDevices"),
                WarmupDuration = ConfigurationHelper.GetConfigValue("Simulator.WarmupDuration", TimeSpan.FromSeconds(30)),
                Scenario = ConfigurationHelper.GetConfigValue<string>("Simulator.Scenario", String.Empty),
                EventLevel = ConfigurationHelper.GetConfigValue<EventLevel>("Simulator.LogLevel", EventLevel.Informational)
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