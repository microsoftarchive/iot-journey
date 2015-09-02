// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.Practices.IoTJourney.ColdStorage.EventProcessor;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Microsoft.Practices.IoTJourney.ColdStorage.EventProcessor.WorkerRole
{
    public class WorkerRole : RoleEntryPoint, IDisposable
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
                Trace.TraceInformation("ColdStorage.EventProcessor.WorkerRole is running");
                _cancellationTokenSource.Token.WaitHandle.WaitOne();
                _coordinator.Dispose();
                Trace.TraceInformation("ColdStorage.EventProcessor.WorkerRole is complete");
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
                Trace.TraceInformation("ColdStorage.EventProcessor.WorkerRole is stopping");

                _cancellationTokenSource.Cancel();
                _runCompleteEvent.WaitOne();

                base.OnStop();
                Trace.TraceInformation("ColdStorage.EventProcessor.WorkerRole has stopped");
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