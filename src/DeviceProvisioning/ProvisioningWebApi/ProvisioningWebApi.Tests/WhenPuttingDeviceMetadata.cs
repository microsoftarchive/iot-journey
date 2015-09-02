// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Practices.IoTJourney.ProvisioningWebApi.DeviceRegistry;
using Microsoft.Practices.IoTJourney.DeviceProvisioningModels;
using Moq;
using Microsoft.Practices.IoTJourney.ProvisioningWebApi.Controllers;
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
    public class WhenPuttingDeviceMetadata
    {
        private RegistryController _controller; 
        private Mock<IDeviceRegistry> _registry;

        public WhenPuttingDeviceMetadata()
        {

            _registry = new Mock<IDeviceRegistry>();
            _registry.Setup(x => x.AddOrUpdateAsync(It.IsAny<DeviceInfo>()))
                .ReturnsAsync(true);

            _controller = new RegistryController(_registry.Object);
        }

        [Fact]
        public async Task NullMetadataReturnsBadRequest()
        {
            IHttpActionResult actionResult = await _controller.PutDeviceMetadata("1", null);
            Assert.IsType<BadRequestErrorMessageResult>(actionResult);
        }

        [Fact]
        public async Task NewDeviceReturnsCreated()
        {
            string id = "111";
            var metadata = new DeviceMetadata();

            IHttpActionResult actionResult = await _controller.PutDeviceMetadata(id, metadata);
            var createdResult = actionResult as CreatedAtRouteNegotiatedContentResult<DeviceInfo>;

            Assert.NotNull(createdResult);
            Assert.Equal("DefaultApi", createdResult.RouteName);
            Assert.Equal(id, createdResult.RouteValues["id"]);
        }

        [Fact]
        public async Task ExistingDeviceReturnsOk()
        {
            string id = "112";
            var metadata = new DeviceMetadata();

            _registry.Setup(x => x.FindAsync(id))
                .ReturnsAsync(new DeviceInfo { DeviceId = id });

            IHttpActionResult actionResult = await _controller.PutDeviceMetadata(id, metadata);
            var contentResult = actionResult as OkNegotiatedContentResult<DeviceInfo>;

            Assert.NotNull(contentResult);
            Assert.NotNull(contentResult.Content);
        }
    }
}
