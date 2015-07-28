// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;
using Microsoft.Practices.IoTJourney.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator.ConsoleHost
{
    internal class Program
    {
        private static FileSystemWatcher _fileSystemWatcher;

        private static SimulationProfile _deviceSimulator;

        private static void Main(string[] args)
        {
            var observableEventListener = new ObservableEventListener();

            var configuration = SimulatorConfiguration.GetCurrentConfiguration();

            observableEventListener.EnableEvents(ScenarioSimulatorEventSource.Log, configuration.GetLogLevel());

            observableEventListener.LogToConsole();

            _deviceSimulator = new SimulationProfile("Console", configuration);

            // check for scenario specified on the command line
            if (args.Length > 0)
            {
                var scenario = args.Contains("/default", StringComparer.OrdinalIgnoreCase)
                    ? SimulationScenarios.DefaultScenario()
                    : args.First(x => !x.StartsWith("/"));

                var ct = args.Contains("/webjob", StringComparer.OrdinalIgnoreCase)
                    ? GetWebJobCancellationToken()
                    : CancellationToken.None;

                _deviceSimulator.RunSimulationAsync(scenario, ct).Wait();
                return;
            }

            var options = new Dictionary<string, Func<CancellationToken, Task>>();

            options.Add("Provision Devices", ProvisionDevicesAsync);

            // no command line arguments, so prompt with a menu.
            foreach (var scenario in SimulationScenarios.AllScenarios)
            {
                options.Add("Run " + scenario, (Func<CancellationToken, Task>)(token => _deviceSimulator.RunSimulationAsync(scenario, token)));
            }

            //options.Add("Deprovision Devices", DeprovisionDevicesAsync);

            Tests.Common.ConsoleHost.RunWithOptionsAsync(options).Wait();
        }

        private static async Task ProvisionDevicesAsync(CancellationToken token)
        {
            _deviceSimulator.ProvisionDevices(true);

            await Task.Delay(0);
        }

        //Uncomment this code when implementing deprovisioning.
        //private static async Task DeprovisionDevicesAsync(CancellationToken token)
        //{

        //}

        private static CancellationToken GetWebJobCancellationToken()
        {
            // See: http://blog.amitapple.com/post/2014/05/webjobs-graceful-shutdown

            var shutdownFile = Environment.GetEnvironmentVariable("WEBJOBS_SHUTDOWN_FILE");
            var directory = Path.GetDirectoryName(shutdownFile);
            if (directory == null)
                return CancellationToken.None;

            var cts = new CancellationTokenSource();
            _fileSystemWatcher = new FileSystemWatcher(directory);
            _fileSystemWatcher.Created += (sender, args) =>
            {
                if (args.FullPath.Equals(Path.GetFullPath(shutdownFile), StringComparison.OrdinalIgnoreCase))
                    cts.Cancel();
            };

            _fileSystemWatcher.EnableRaisingEvents = true;
            return cts.Token;
        }
    }
}
