// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DeviceProvisioning.AccessTokens;
using DeviceProvisioning.Controllers;
using DeviceProvisioning.DeviceRegistry;
using Microsoft.Practices.IoTJourney.DeviceProvisioningModels;
using Moq;
using System;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Xunit;

namespace DeviceProvisioning.Tests
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
            Device device = new Device { DeviceId = "1" };
            DeviceInfo info = new DeviceInfo { DeviceId = "1" };

            _registry.Setup(x => x.FindAsync("1"))
                .ReturnsAsync(info);

            IHttpActionResult actionResult = await _controller.ProvisionDevice(device);

            var contentResult = actionResult as OkNegotiatedContentResult<DeviceEndpoint>;

            Assert.NotNull(contentResult);
            Assert.NotNull(contentResult.Content);
            Assert.Equal(TOKEN, contentResult.Content.AccessToken);
        }

        [Fact]
        public async Task NoDeviceReturnsNotFound()
        {
            Device device = new Device { DeviceId = "2" };

            IHttpActionResult actionResult = await _controller.ProvisionDevice(device);

            Assert.NotNull(actionResult);
            Assert.IsType<NotFoundResult>(actionResult);
        }


        [Fact]
        public async Task NullModelReturnsBadRequest()
        {
            IHttpActionResult actionResult = await _controller.ProvisionDevice(null);
            Assert.IsType<BadRequestErrorMessageResult>(actionResult);
        }

        [Fact]
        public async Task InvalidModelReturnsBadRequest()
        {
            Device device = new Device { DeviceId = "1" };

            _controller.ModelState.AddModelError("FakeError", "FakeError");

            IHttpActionResult actionResult = await _controller.ProvisionDevice(device);
            Assert.IsType<InvalidModelStateResult>(actionResult);
        }
    }
}
