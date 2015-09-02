using Microsoft.Practices.IoTJourney.ProvisioningWebApi.AccessTokens;
using Microsoft.Practices.IoTJourney.ProvisioningWebApi.Controllers;
using Microsoft.Practices.IoTJourney.ProvisioningWebApi.DeviceRegistry;
using Microsoft.Practices.IoTJourney.DeviceProvisioningModels;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Xunit;

namespace Microsoft.Practices.IoTJourney.ProvisioningWebApi.Tests
{
    public class WhenRevoking
    {
        private ProvisionController _controller;
        private Mock<ITokenProvider> _tokenProvider;
        private Mock<IDeviceRegistry> _registry;

        public WhenRevoking()
        {
            _tokenProvider = new Mock<ITokenProvider>();
 
            _registry = new Mock<IDeviceRegistry>();
            _registry.Setup(x => x.AddOrUpdateAsync(It.IsAny<DeviceInfo>()))
                .ReturnsAsync(true);
            
            _controller = new ProvisionController(_tokenProvider.Object, _registry.Object);
        }

        [Fact]
        public async Task ReturnsDeviceInfo()
        {
            string deviceId = "1";

            DeviceInfo info = new DeviceInfo { 
                DeviceId = deviceId,
                Status = DeviceStateConstants.RegisteredState
            };

            _registry.Setup(x => x.FindAsync(deviceId))
                .ReturnsAsync(info);

            IHttpActionResult actionResult = await _controller.RevokeDevice(deviceId);

            var contentResult = actionResult as OkNegotiatedContentResult<DeviceInfo>;

            Assert.NotNull(contentResult);
            Assert.NotNull(contentResult.Content);
        }

        [Fact]
        public async Task NoMatchingDeviceReturnsNotFound()
        {
            string deviceId = "2";
            IHttpActionResult actionResult = await _controller.RevokeDevice(deviceId);

            Assert.NotNull(actionResult);
            Assert.IsType<NotFoundResult>(actionResult);
        }    
    }
}
