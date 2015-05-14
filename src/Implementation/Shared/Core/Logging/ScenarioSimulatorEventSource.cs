using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Practices.IoTJourney.Logging
{
    public class ScenarioSimulatorEventSource : EventSource
    {
        public static ScenarioSimulatorEventSource Log = new ScenarioSimulatorEventSource();

        public void InstrumentationDisabled(string instanceName)
        {
            WriteEvent(1, instanceName);
        }

        [NonEvent]
        public void InstallingPerformanceCountersFailed(Exception exception, string instanceName)
        {
            InstallingPerformanceCountersFailed_Inner(exception.ToString(), instanceName);
        }

        private void InstallingPerformanceCountersFailed_Inner(string exceptionString, string instanceName)
        {
            WriteEvent(2, exceptionString, instanceName);
        }

        [NonEvent]
        public void InitializingPerformanceCountersFailed(Exception exception, string instanceName)
        {
            InitializingPerformanceCountersFailed_Inner(exception.ToString(), instanceName);
        }

        private void InitializingPerformanceCountersFailed_Inner(string exceptionString, string instanceName)
        {
            WriteEvent(3, exceptionString, instanceName);
        }

        [NonEvent]
        public void CreatingPerfCounterCategoryFailed(Exception exception, string categoryName)
        {
            CreatingPerfCounterCategoryFailed_Inner(exception.ToString(), categoryName);
        }

        private void CreatingPerfCounterCategoryFailed_Inner(string exceptionString, string categoryName)
        {
            WriteEvent(4, exceptionString, categoryName);
        }

        public void SimulationStarted(string hostName, string scenario)
        {
            WriteEvent(5, hostName, scenario);
        }

        public void SimulationEnded(string hostName)
        {
            WriteEvent(6, hostName);
        }

        public void WarmingUpFor(string deviceId, long waitBeforeStartingTicks)
        {
            WriteEvent(7, deviceId, waitBeforeStartingTicks);
        }

        [NonEvent]
        public void UnableToSend(Exception exception, string partitionKey, object evt)
        {
            UnableToSend_inner(exception.ToString(), partitionKey, evt.ToString());
        }

        private void UnableToSend_inner(string exceptionString, string partitionKey, string objectString)
        {
            WriteEvent(8, exceptionString, partitionKey, objectString);
        }

        [NonEvent]
        public void ServiceThrottled(Exception exception, string partitionKey)
        {
            ServiceThrottled_Inner(exception.ToString(), partitionKey);
        }

        private void ServiceThrottled_Inner(string exceptionString, string partitionKey)
        {
            WriteEvent(9, exceptionString, partitionKey);
        }

        public void DeviceStarting(string deviceId)
        {
            WriteEvent(10, deviceId);
        }

        [NonEvent]
        public void DeviceUnexpectedFailure(Exception exception, string deviceId)
        {
            DeviceUnexpectedFailure_Inner(exception.ToString(), deviceId);
        }

        private void DeviceUnexpectedFailure_Inner(string exceptionString, string deviceId)
        {
            WriteEvent(11, exceptionString, deviceId);
        }
        public void DeviceStopping(string deviceId)
        {
            WriteEvent(12, deviceId);
        }

        public void TotalSimulationTook(long elapsedTimeTicks)
        {
            WriteEvent(13, elapsedTimeTicks);
        }

        public void SpinningAfterScenario()
        {
            WriteEvent(14);
        }

        [NonEvent]
        public void UnknownScenario(string scenario, Exception exception)
        {
            UnknownScenario_Inner(scenario, exception.ToString());
        }

        private void UnknownScenario_Inner(string scenario, string exceptionString)
        {
            WriteEvent(15, scenario, exceptionString);
        }

        public void EventSent(long timeSpanTicks, string partitionKey)
        {
            WriteEvent(16, timeSpanTicks, partitionKey);
        }

        public void FinalEventCountForAllDevices(int eventCount)
        {
            WriteEvent(17, eventCount);
        }

        public void CurrentEventCountForAllDevices(int eventCount)
        {
            WriteEvent(18, eventCount);
        }

        public void FinalEventCount(string deviceId, int eventCount)
        {
            WriteEvent(19, deviceId, eventCount);
        }
    }
}
