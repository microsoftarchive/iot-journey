using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Microsoft.Practices.IoTJourney.WarmStorage.EventProcessor.WorkerRole
{
    public class WorkerRole : RoleEntryPoint, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent _runCompleteEvent = new ManualResetEvent(false);

        private WarmStorageCoordinator _coordinator;

        public override bool OnStart()
        {
            try
            {
                // Set up the process defaults for connections to optimize storage performance
                ServicePointManager.DefaultConnectionLimit = int.MaxValue;

                var configuration = Configuration.GetCurrentConfiguration();

                _coordinator =
                    WarmStorageCoordinator.CreateAsync(RoleEnvironment.CurrentRoleInstance.Id, configuration).Result;

                return base.OnStart();
            }
            catch (AggregateException ae)
            {
                ae.Handle(ex =>
                {
                    Trace.TraceError(ex.ToString());

                    return false;
                });

                throw;
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
                Trace.TraceInformation("WarmStorage.EventProcessor.WorkerRole is running");
                _cancellationTokenSource.Token.WaitHandle.WaitOne();
                _coordinator.Dispose();
                Trace.TraceInformation("WarmStorage.EventProcessor.WorkerRole is complete");
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
                Trace.TraceInformation("WarmStorage.EventProcessor.WorkerRole is stopping");

                _cancellationTokenSource.Cancel();
                _runCompleteEvent.WaitOne();

                base.OnStop();
                Trace.TraceInformation("WarmStorage.EventProcessor.WorkerRole has stopped");
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                throw;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
            _runCompleteEvent.Dispose();
        }
    }
}