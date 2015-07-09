using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.Tests.Common.Mocks;
using Microsoft.Practices.IoTJourney.WarmStorage.ElasticSearchWriter;
using Microsoft.ServiceBus.Messaging;
using Moq;
using Xunit;

namespace Microsoft.Practices.IoTJourney.WarmStorage.Tests
{
    public class GivenAWarmStorageProcessor
    {
        private readonly WarmStorageProcessor _processor;
        private Mock<IElasticSearchWriter> _writerMock;
        private Mock<IBuildingLookupService> _lookupServiceMock;

        public GivenAWarmStorageProcessor()
        {
            _writerMock = new Mock<IElasticSearchWriter>();
            _lookupServiceMock = new Mock<IBuildingLookupService>();
            _processor = new WarmStorageProcessor(n => _writerMock.Object, "eventhubname", _lookupServiceMock.Object);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ProcessEventsAsyncSendsEventDataToWriter()
        {
            var context = MockPartitionContext.CreateWithNoopCheckpoint("0");
            await _processor.OpenAsync(context);

            var eventData = new EventData(Encoding.UTF8.GetBytes("{ \"property1\": \"value1\"}"));

            var eventDataArray = new[]
            {
                eventData    
            };

            await _processor.ProcessEventsAsync(context, eventDataArray);

            _writerMock.Verify(
                w => w.WriteAsync(It.Is<List<EventData>>(p => p.Contains(eventData)), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task BuildingIdPropertyPopulatedFromLookupService()
        {
            _lookupServiceMock.Setup(service => service.GetBuildingIdAsync("123")).Returns(()=>Task.FromResult("456"));
            var context = MockPartitionContext.CreateWithNoopCheckpoint("0");
            await _processor.OpenAsync(context);

            var eventData = new EventData(Encoding.UTF8.GetBytes("{ \"DeviceId\": \"123\"}"));

            var eventDataArray = new[]
            {
                eventData    
            };

            await _processor.ProcessEventsAsync(context, eventDataArray);

            _writerMock.Verify(
                w => w.WriteAsync(It.Is<List<EventData>>(p => p.First().Properties["BuildingId"] == "456"), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
