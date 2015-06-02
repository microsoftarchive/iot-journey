// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.IoTJourney.Devices.Events;
using Microsoft.Practices.IoTJourney.Logging;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator
{
    using EventGenerator = Func<EventEntry[]>;

    public static class SimulationScenarios
    {
        private static readonly Dictionary<string, EventGenerator> ScenarioMap;

        static SimulationScenarios()
        {
            ScenarioMap =
                typeof(SimulationScenarios).GetTypeInfo()
                    .DeclaredMethods
                    .Where(x => x.ReturnType == typeof(EventEntry[]))
                    .ToDictionary(
                        x => x.Name,
                        x => (EventGenerator)x.CreateDelegate(typeof(EventGenerator)));
        }

        public static EventEntry[] NoErrorsExpected()
        {
            return new[]
                       {
                           new EventEntry(EventFactory.TemperatureEventFactory, TimeSpan.FromSeconds(1), 0.1) 
                       };
        }

        public static EventEntry[] ThrirtyDegreeReadings()
        {
            return new[]
                       {
                           new EventEntry(EventFactory.ThirtyDegreeTemperatureEventFactory, TimeSpan.FromSeconds(10), 0.1) 
                       };
        }

        public static IReadOnlyList<string> AllScenarios
        {
            get { return ScenarioMap.Keys.ToList(); }
        }

        public static EventGenerator GetScenarioByName(string scenario)
        {
            EventGenerator generator;
            if (!ScenarioMap.TryGetValue(scenario, out generator))
            {
                var ex = new KeyNotFoundException("The specified scenario, " + scenario + ", was not recognized.");
                ScenarioSimulatorEventSource.Log.UnknownScenario(scenario, ex);
                throw ex;
            }

            return generator;
        }

        public static string DefaultScenario()
        {
            EventGenerator func = NoErrorsExpected;
            return func.Method.Name;
        }
    }
}