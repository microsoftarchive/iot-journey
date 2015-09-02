// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Practices.IoTJourney.ProvisioningWebApi.AccessTokens;
using Microsoft.Practices.IoTJourney.ProvisioningWebApi.Controllers;
using Microsoft.Practices.IoTJourney.ProvisioningWebApi.DeviceRegistry;
using Microsoft.Practices.IoTJourney.DeviceProvisioningModels;
using Moq;
using System;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Xunit;

namespace Microsoft.Practices.IoTJourney.ProvisioningWebApi.Tests
{
    public class WhenProvisioning
    {
        private ProvisionController _controller;
        private Mock<ITokenProvider> _tokenProvider;
        private Mock<IDeviceRegistry> _registry;

        const string TOKEN = "TOKEN";

        public WhenProvisioning()
        {
            _tokenProvider = new Mock<ITokenProvider>();
            _tokenProvider.Setup(x => x.GetTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(TOKEN);
            _tokenProvider.Setup(x => x.EndpointUri).Returns(new Uri("http://example.com"));

            _registry = new Mock<IDeviceRegistry>();
            _registry.Setup(x => x.AddOrUpdateAsync(It.IsAny<DeviceInfo>()))
                .ReturnsAsync(true);
            
            _controller = new ProvisionController(_tokenProvider.Object, _registry.Object);
        }

        [Fact]
        public async Task ReturnsToken()
        {
            string deviceId = "1";

            DeviceInfo info = new DeviceInfo { 
                DeviceId = deviceId,
                Status = DeviceStateConstants.RegisteredState
            };

            _registry.Setup(x => x.FindAsync(deviceId))
                .ReturnsAsync(info);

            IHttpActionResult actionResult = await _controller.ProvisionDevice(deviceId);

            var contentResult = actionResult as OkNegotiatedContentResult<DeviceEndpoint>;

            Assert.NotNull(contentResult);
            Assert.NotNull(contentResult.Content);
            Assert.Equal(TOKEN, contentResult.Content.AccessToken);
        }

        [Fact]
        public async Task RestoresDeviceIfRevoked()
        {
            string deviceId = "1";

            DeviceInfo info = new DeviceInfo
            {
                DeviceId = deviceId,
                Status = DeviceStateConstants.RevokedState 
            };

            _registry.Setup(x => x.FindAsync(deviceId))
                .ReturnsAsync(info);

            IHttpActionResult actionResult = await _controller.ProvisionDevice(deviceId);

            _tokenProvider.Verify(x => x.RestoreDeviceAsync(deviceId));
        }

        [Fact]
        public async Task NoMatchingDeviceReturnsNotFound()
        {
            string deviceId = "2";
            IHttpActionResult actionResult = await _controller.ProvisionDevice(deviceId);

            Assert.NotNull(actionResult);
            Assert.IsType<NotFoundResult>(actionResult);
        }
    }
}
