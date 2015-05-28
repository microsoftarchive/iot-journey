// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using Microsoft.Practices.IoTJourney.ColdStorage.Logging;
using Xunit;

namespace Microsoft.Practices.IoTJourney.ColdStorage.Tests
{
    public class GivenAColdStorageEventSource
    {
        [Fact]
        public void CustomEventSourcePassesValidation()
        {
            EventSourceAnalyzer.InspectAll(ColdStorageEventSource.Log);
        }
    }
}
