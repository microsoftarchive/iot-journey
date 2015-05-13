// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Practices.IoTJourney.Logging
{
    public abstract class LogHelpers
    {
        public static void HandleRoleException(ILogger log, string roleMethod, Exception ex)
        {
            IEnumerable<Exception> exceptions;

            if (ex is AggregateException)
                exceptions = (ex as AggregateException).Flatten().InnerExceptions;
            else
                exceptions = new Exception[] {ex};

            foreach (var e in exceptions)
            {
                EventLog.WriteEntry("Application Error", e.ToString(), EventLogEntryType.Error);
                log.Error(e, "Could not run " + roleMethod);
            }
        }
    }
}
