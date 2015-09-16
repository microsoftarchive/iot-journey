// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor.ConsoleHost.Formatters
{
    public interface IEventTextFormatter
    {
        /// <summary>
        ///     Writes the event.
        /// </summary>
        /// <param name="snapshot">The event entry.</param>
        /// <param name="writer">The writer.</param>
        void WriteEvent(PartitionSnapshot snapshot, TextWriter writer);
    }
}