// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Practices.IoTJourney.Logging;

namespace Microsoft.Practices.IoTJourney.Instrumentation
{
    public static class LoggerPerformanceCounterExtensions
    {
        public static void InstrumentationDisabled(this ILogger logger, string instanceName)
        {
            logger.Info("Instrumentation disabled for {0}", instanceName);
        }

        public static void InstallingPerformanceCountersFailed(
            this ILogger logger,
            string instanceName,
            Exception exception)
        {
            logger.Warning(
                "Installing performance counters for {0} failed. Instrumentation disabled",
                instanceName);
        }

        public static void InitializingPerformanceCountersFailed(
            this ILogger logger,
            string instanceName,
            Exception exception)
        {
            logger.Warning(
                exception,
                "Initializing performance counters for {0} failed. Instrumentation disabled",
                instanceName);
        }
    }
}