using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Practices.IoTJourney.Logging
{
    public class ScenarioSimulatorEventSource : EventSource
    {
        public static ScenarioSimulatorEventSource Log = new ScenarioSimulatorEventSource();

        [Event(1, Level = EventLevel.Informational)]
        public void InstrumentationDisabled(string instanceName)
        {
            WriteEvent(1, instanceName);
        }

        [NonEvent]
        public void InstallingPerformanceCountersFailed(Exception exception, string instanceName)
        {
            InstallingPerformanceCountersFailed_Inner(exception.ToString(), instanceName);
        }

        [Event(2, Level = EventLevel.Error)]
        private void InstallingPerformanceCountersFailed_Inner(string exceptionString, string instanceName)
        {
            WriteEvent(2, exceptionString, instanceName);
        }

        [NonEvent]
        public void InitializingPerformanceCountersFailed(Exception exception, string instanceName)
        {
            InitializingPerformanceCountersFailed_Inner(exception.ToString(), instanceName);
        }

        [Event(3, Level = EventLevel.Error)]
        private void InitializingPerformanceCountersFailed_Inner(string exceptionString, string instanceName)
        {
            WriteEvent(3, exceptionString, instanceName);
        }

        [NonEvent]
        public void CreatingPerfCounterCategoryFailed(Exception exception, string categoryName)
        {
            CreatingPerfCounterCategoryFailed_Inner(exception.ToString(), categoryName);
        }

        [Event(4, Level = EventLevel.Error)]
        private void CreatingPerfCounterCategoryFailed_Inner(string exceptionString, string categoryName)
        {
            WriteEvent(4, exceptionString, categoryName);
        }

        [Event(5, Level = EventLevel.Informational)]
        public void SimulationStarted(string hostName, string scenario)
        {
            WriteEvent(5, hostName, scenario);
        }

        [Event(6, Level = EventLevel.Informational)]
        public void SimulationEnded(string hostName)
        {
            WriteEvent(6, hostName);
        }

        [Event(7, Level = EventLevel.Verbose)]
        public void WarmingUpFor(string deviceId, long waitBeforeStartingTicks)
        {
            WriteEvent(7, deviceId, waitBeforeStartingTicks);
        }

        [NonEvent]
        public void UnableToSend(Exception exception, object evt)
        {
            UnableToSend_inner(exception.ToString(), evt.ToString());
        }

        [Event(8, Level = EventLevel.Error)]
        private void UnableToSend_inner(string exceptionString, string objectString)
        {
            WriteEvent(8, exceptionString, objectString);
        }

        [NonEvent]
        public void ServiceThrottled(Exception exception)
        {
            ServiceThrottled_Inner(exception.ToString());
        }

        [Event(9, Level = EventLevel.Error)]
        private void ServiceThrottled_Inner(string exceptionString)
        {
            WriteEvent(9, exceptionString);
        }

        [Event(10, Level = EventLevel.Informational)]
        public void DeviceStarting(string deviceId)
        {
            WriteEvent(10, deviceId);
        }

        [NonEvent]
        public void DeviceUnexpectedFailure(Exception exception, string deviceId)
        {
            DeviceUnexpectedFailure_Inner(exception.ToString(), deviceId);
        }

        [Event(11, Level = EventLevel.Error)]
        private void DeviceUnexpectedFailure_Inner(string exceptionString, string deviceId)
        {
            WriteEvent(11, exceptionString, deviceId);
        }

        [Event(12, Level = EventLevel.Informational)]
        public void DeviceStopping(string deviceId)
        {
            WriteEvent(12, deviceId);
        }

        [Event(13, Level = EventLevel.Informational)]
        public void TotalSimulationTook(long elapsedTimeTicks)
        {
            WriteEvent(13, elapsedTimeTicks);
        }

        [Event(14, Level = EventLevel.Informational)]
        public void SpinningAfterScenario()
        {
            WriteEvent(14);
        }

        [NonEvent]
        public void UnknownScenario(string scenario, Exception exception)
        {
            UnknownScenario_Inner(scenario, exception.ToString());
        }

        [Event(15, Level = EventLevel.Error)]
        private void UnknownScenario_Inner(string scenario, string exceptionString)
        {
            WriteEvent(15, scenario, exceptionString);
        }

        [Event(16, Level = EventLevel.Verbose)]
        public void EventSent(long timeSpanTicks)
        {
            WriteEvent(16, timeSpanTicks);
        }

        [Event(17, Level = EventLevel.Informational)]
        public void FinalEventCountForAllDevices(int eventCount)
        {
            WriteEvent(17, eventCount);
        }

        [Event(18, Level = EventLevel.Informational)]
        public void CurrentEventCountForAllDevices(int eventCount)
        {
            WriteEvent(18, eventCount);
        }

        [Event(19, Level = EventLevel.Informational)]
        public void FinalEventCount(string deviceId, int eventCount)
        {
            WriteEvent(19, deviceId, eventCount);
        }

        [Event(20, Level = EventLevel.Informational)]
        public void CurrentEventsPerSecond(string EventsPerSecond)
        {
            WriteEvent(20, EventsPerSecond);
        }

    }
}
