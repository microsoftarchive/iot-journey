// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Practices.IoTJourney.Instrumentation
{
    public class PerformanceCounterDefinition
    {
        private readonly string _categoryName;
        private readonly string _counterName;
        private readonly string _counterHelp;
        private readonly PerformanceCounterType _counterType;

        internal PerformanceCounterDefinition(string categoryName, string counterName, string counterHelp, PerformanceCounterType counterType)
        {
            _categoryName = categoryName;
            _counterName = counterName;
            _counterHelp = counterHelp;
            _counterType = counterType;
        }

        public PerformanceCounter CreatePerformanceCounter(string instanceName)
        {
            return new PerformanceCounter(_categoryName, _counterName, instanceName, false);
        }

        internal CounterCreationData GetCreationData()
        {
            return new CounterCreationData(_counterName, _counterHelp, _counterType);
        }
    }
}
