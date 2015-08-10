using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Practices.IoTJourney.ScenarioSimulator;
using Microsoft.Practices.IoTJourney;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator.WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent _runCompleteEvent = new ManualResetEvent(false);
        private SimulationProfile _deviceSimulator;

        public override bool OnStart()
        {
            try
            {
                var configuration = SimulatorConfiguration.GetCurrentConfiguration();
                var hostName = ConfigurationHelper.SourceName;
                _deviceSimulator = new SimulationProfile(hostName, configuration);  
                
                _deviceSimulator.ProvisionDevices(true);

                return base.OnStart();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());

                throw;
            }
        }

        public override void Run()
        {
            try
            {
                Trace.TraceInformation("ScenarioSimulator.WorkerRole is running");
                var scenario = SimulationScenarios.DefaultScenario();
                _deviceSimulator.RunSimulationAsync(scenario, _cancellationTokenSource.Token).Wait();
                Trace.TraceInformation("ScenarioSimulator.WorkerRole is complete");
            }
            catch (Exception ex)
            {
                 Trace.TraceError(ex.ToString());
            }
            finally
            {
                _runCompleteEvent.Set();
            }
        }

        public override void OnStop()
        {
            try
            {
                Trace.TraceInformation("ScenarioSimulator.WorkerRole is stopping");

                _cancellationTokenSource.Cancel();
                _runCompleteEvent.WaitOne();

                base.OnStop();
                Trace.TraceInformation("ScenarioSimulator.WorkerRole has stopped");
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                throw;
            }
        }
    }
}
