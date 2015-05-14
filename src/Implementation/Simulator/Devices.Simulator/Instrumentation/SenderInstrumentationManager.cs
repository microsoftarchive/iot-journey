// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Microsoft.Practices.IoTJourney.Instrumentation;
using Microsoft.Practices.IoTJourney.Logging;

namespace Microsoft.Practices.IoTJourney.Devices.Simulator.Instrumentation
{
    public class SenderInstrumentationManager : InstrumentationManager
    {
        private const string SenderPerformanceCounterCategoryName = "EventHub Sender";

        private bool _instrumentationEnabled;

        public SenderInstrumentationManager(
            bool instrumentationEnabled = false,
            bool installInstrumentation = false)
            : base(SenderPerformanceCounterCategoryName, "", PerformanceCounterCategoryType.MultiInstance)
        {
            _instrumentationEnabled = instrumentationEnabled;

            TotalEventsSentCounterDefinition = this.AddDefinition(
                "Total events sent",
                "total events sent",
                PerformanceCounterType.NumberOfItems64);
            TotalEventsRequestedCounterDefinition = this.AddDefinition(
                "Total events requested",
                "total events sent",
                PerformanceCounterType.NumberOfItems64);
            EventsSentPerSecondCounterDefinition = this.AddDefinition(
                "Events sent per sec",
                "events per second sent",
                PerformanceCounterType.RateOfCountsPerSecond64);
            EventsRequestedPerSecondCounterDefinition = this.AddDefinition(
                "Events requested per sec",
                string.Empty,
                PerformanceCounterType.RateOfCountsPerSecond64);
            TotalBytesSentCounterDefinition = this.AddDefinition(
                "total bytes sent",
                "total bytes sent",
                PerformanceCounterType.NumberOfItems64);
            BytesSentPerSecondCounterDefinition = this.AddDefinition(
                "Bytes sent per sec",
                "bytes per second sent",
                PerformanceCounterType.RateOfCountsPerSecond64);
            AverageEventSendingTimeCounterDefinition = this.AddDefinition(
                "Avg. event sending time",
                string.Empty,
                PerformanceCounterType.RawFraction);
            AverageEventSendingTimeBaseCounterDefinition = this.AddDefinition(
                "Avg. event sending time base",
                string.Empty,
                PerformanceCounterType.RawBase);

            if (installInstrumentation)
            {
                CreateCounters();
            }
        }

        public ISenderInstrumentationPublisher CreatePublisher(string instanceName)
        {
            if (!_instrumentationEnabled)
            {
                ScenarioSimulatorEventSource.Log.InstrumentationDisabled(instanceName);
                return new NullSenderInstrumentationPublisher();
            }

            try
            {
                return new SenderInstrumentationPublisher(instanceName, this);
            }
            catch (Exception ex)
            {
                ScenarioSimulatorEventSource.Log.InitializingPerformanceCountersFailed(ex, instanceName);
                return new NullSenderInstrumentationPublisher();
            }
        }

        internal PerformanceCounterDefinition TotalEventsSentCounterDefinition { get; private set; }

        internal PerformanceCounterDefinition TotalEventsRequestedCounterDefinition { get; private set; }

        internal PerformanceCounterDefinition EventsSentPerSecondCounterDefinition { get; private set; }

        internal PerformanceCounterDefinition EventsRequestedPerSecondCounterDefinition { get; private set; }

        internal PerformanceCounterDefinition TotalBytesSentCounterDefinition { get; private set; }

        internal PerformanceCounterDefinition BytesSentPerSecondCounterDefinition { get; private set; }

        internal PerformanceCounterDefinition AverageEventSendingTimeCounterDefinition { get; private set; }

        internal PerformanceCounterDefinition AverageEventSendingTimeBaseCounterDefinition { get; private set; }

        private class NullSenderInstrumentationPublisher : ISenderInstrumentationPublisher
        {
            public void EventSendRequested()
            {
            }

            public void EventSendCompleted(long length, TimeSpan elapsed)
            {
            }
        }
    }
}