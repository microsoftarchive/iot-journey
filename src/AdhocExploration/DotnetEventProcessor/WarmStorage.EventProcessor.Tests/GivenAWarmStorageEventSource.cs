using Microsoft.Practices.EnterpriseLibrary.SemanticLogging.Utility;
using Microsoft.Practices.IoTJourney.WarmStorage.Logging;
using Xunit;

namespace Microsoft.Practices.IoTJourney.WarmStorage.Tests
{
    public class GivenAWarmStorageEventSource
    {
        [Fact]
        public void CustomEventSourcePassesValidation()
        {
            EventSourceAnalyzer.InspectAll(WarmStorageEventSource.Log);
        }
    }
}
