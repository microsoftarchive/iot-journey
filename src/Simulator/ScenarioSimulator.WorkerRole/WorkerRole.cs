// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator.WorkerRole
{
    public class WorkerRole : RoleEntryPoint, IDisposable
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

        public void Dispose()
        {
            _deviceSimulator.Dispose();
            _cancellationTokenSource.Dispose();
            _runCompleteEvent.Dispose();
        }
    }
}
