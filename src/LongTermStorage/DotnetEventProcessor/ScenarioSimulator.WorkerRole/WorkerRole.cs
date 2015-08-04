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
using Microsoft.Practices.IoTJourney;
using Microsoft.Practices.IoTJourney.ColdStorage.EventProcessor;
using Microsoft.Practices.IoTJourney.ColdStorage;

namespace Microsoft.Practices.IoTJourney.ColdStorage.ScenarioSimulator.WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent _runCompleteEvent = new ManualResetEvent(false);

        private ColdStorageCoordinator _coordinator;

        public override bool OnStart()
        {
            try
            {
                // Set up the process defaults for connections to optimize storage performance
                ServicePointManager.DefaultConnectionLimit = int.MaxValue;

                var configuration = Configuration.GetCurrentConfiguration();

                _coordinator = ColdStorageCoordinator.CreateAsync(RoleEnvironment.CurrentRoleInstance.Id, configuration).Result;

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
                _cancellationTokenSource.Token.WaitHandle.WaitOne();
                _coordinator.Dispose();
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
