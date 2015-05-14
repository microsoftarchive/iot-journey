// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator.Instrumentation
{
    internal class SenderInstrumentationPublisher : ISenderInstrumentationPublisher
    {
        private readonly PerformanceCounter _totalEventsSentCounter;
        private readonly PerformanceCounter _totalEventsRequestedCounter;
        private readonly PerformanceCounter _eventsSentPerSecondCounter;
        private readonly PerformanceCounter _eventsRequestedPerSecondCounter;
        private readonly PerformanceCounter _totalBytesSentCounter;
        private readonly PerformanceCounter _bytesPerSecondSentCounter;
        private readonly PerformanceCounter _averageEventSendingTimeCounter;
        private readonly PerformanceCounter _averageEventSendingTimeBaseCounter;

        internal SenderInstrumentationPublisher(string instanceName, SenderInstrumentationManager instrumentationManager)
        {
            _totalEventsSentCounter = 
                instrumentationManager.TotalEventsSentCounterDefinition.CreatePerformanceCounter(instanceName);
            _totalEventsRequestedCounter =
                instrumentationManager.TotalEventsRequestedCounterDefinition.CreatePerformanceCounter(instanceName);
            _eventsSentPerSecondCounter = 
                instrumentationManager.EventsSentPerSecondCounterDefinition.CreatePerformanceCounter(instanceName);
            _eventsRequestedPerSecondCounter =
                instrumentationManager.EventsRequestedPerSecondCounterDefinition.CreatePerformanceCounter(instanceName);
            _totalBytesSentCounter = 
                instrumentationManager.TotalBytesSentCounterDefinition.CreatePerformanceCounter(instanceName);
            _bytesPerSecondSentCounter = 
                instrumentationManager.BytesSentPerSecondCounterDefinition.CreatePerformanceCounter(instanceName);
            _averageEventSendingTimeCounter = 
                instrumentationManager.AverageEventSendingTimeCounterDefinition.CreatePerformanceCounter(instanceName);
            _averageEventSendingTimeBaseCounter = 
                instrumentationManager.AverageEventSendingTimeBaseCounterDefinition.CreatePerformanceCounter(instanceName);

            _totalEventsSentCounter.RawValue = 0L;
            _totalBytesSentCounter.RawValue = 0L;
            _averageEventSendingTimeCounter.RawValue = 0L;
            _averageEventSendingTimeBaseCounter.RawValue = 0L;
        }

        public void EventSendRequested()
        {
            _totalEventsRequestedCounter.Increment();
            _eventsRequestedPerSecondCounter.Increment();
        }

        public void EventSendCompleted(long length, TimeSpan elapsed)
        {
            _totalEventsSentCounter.Increment();
            _totalBytesSentCounter.IncrementBy(length);
            _eventsSentPerSecondCounter.Increment();
            _bytesPerSecondSentCounter.IncrementBy(length);

            _averageEventSendingTimeCounter.IncrementBy(((long)elapsed.TotalMilliseconds) / 100L);
            _averageEventSendingTimeBaseCounter.Increment();
        }
    }
}