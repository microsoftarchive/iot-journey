// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost.Formatters;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost
{
    public static class PartitionSnapshotExtensions
    {
        /// <summary>
        ///     Formats an <see cref="PartitionSnapshot" /> as a string using an <see cref="IEventTextFormatter" />.
        /// </summary>
        /// <param name="snapshot">The snapshot to format.</param>
        /// <param name="formatter">The formatter to use.</param>
        /// <returns>A formatted snapshot, or <see langword="null" /> if an exception is thrown by the <paramref name="formatter" />.</returns>
        public static string TryFormatAsString(this PartitionSnapshot snapshot, IEventTextFormatter formatter)
        {
            try
            {
                return formatter.WriteEvent(snapshot);
            }
            catch (Exception e)
            {
                //SemanticLoggingEventSource.Log.FormatEntryAsStringFailed(e.ToString());
            }

            return null;
        }
    }
}