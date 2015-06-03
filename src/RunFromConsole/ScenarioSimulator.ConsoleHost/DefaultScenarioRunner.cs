namespace Microsoft.Practices.IoTJourney.ScenarioSimulator.ConsoleHost
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;

    using Microsoft.Practices.IoTJourney.ScenarioSimulator;
    using Microsoft.Practices.IoTJourney.Logging;

    public class DefaultScenarioRunner
    {
        public void Start()
        {
            try
            {
                int instanceCount = 1;

                // Obtain the simulation configuration and generate a simulation profile.  Set up
                // the cancellation token to terminate the simulation after the configured duration 
                var configuration = SimulatorConfiguration.GetCurrentConfiguration();

                var _scenario = SimulationScenarios.DefaultScenario();

                var hostName = ConfigurationHelper.SourceName;

                var _simulationProfile = new SimulationProfile(
                    hostName,
                    instanceCount,
                    configuration);

                var _cancellationTokenSource = new CancellationTokenSource();
                _cancellationTokenSource.CancelAfter(configuration.ScenarioDuration);

                var stopwatch = Stopwatch.StartNew();

                // TODO add log
                // ScenarioSimulatorEventSource.Log.WebJobRunning();

                // Run the scenario for the specified time
                _simulationProfile
                    .RunSimulationAsync(_scenario, _cancellationTokenSource.Token)
                    .Wait();

                stopwatch.Stop();

                ScenarioSimulatorEventSource.Log.TotalSimulationTook(stopwatch.Elapsed.Milliseconds);
                // TODO add log
                // ScenarioSimulatorEventSource.Log.WebJobStopped();

            }
            catch (Exception e)
            {
                // TODO add log
                // ScenarioSimulatorEventSource.Log.HandleWebJobException();
            }
        }
    }
}