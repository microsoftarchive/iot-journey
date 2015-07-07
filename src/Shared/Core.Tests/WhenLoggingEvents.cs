// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using Microsoft.Practices.IoTJourney.Logging;
using Xunit;

namespace Core.Tests
{
    public class WhenLoggingEvents
    {
        [Fact]
        public void CustomEventSourcePassesValidation()
        {
            EventSourceAnalyzer.InspectAll(ScenarioSimulatorEventSource.Log);
        }
    }
}
