// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Practices.IoTJourney.ProvisioningWebApi.DeviceRegistry;
using Microsoft.Practices.IoTJourney.DeviceProvisioningModels;
using Moq;
using Microsoft.Practices.IoTJourney.ProvisioningWebApi.Controllers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Xunit;

namespace Microsoft.Practices.IoTJourney.ProvisioningWebApi.Tests
{
    public class WhenGettingDeviceMetadata
    {
        private RegistryController _controller; 
        private Mock<IDeviceRegistry> _registry;

        const string DEVICE_ID = "111";

        public WhenGettingDeviceMetadata()
        {
            _registry = new Mock<IDeviceRegistry>();

            _registry.Setup(x => x.FindAsync(DEVICE_ID))
                .ReturnsAsync(new DeviceInfo { DeviceId = DEVICE_ID });

            _controller = new RegistryController(_registry.Object);
        }

        [Fact]
        public async Task IfFoundReturnsOk()
        {

            IHttpActionResult actionResult = await _controller.GetDeviceMetadata(DEVICE_ID);
            var contentResult = actionResult as OkNegotiatedContentResult<DeviceInfo>;

            Assert.NotNull(contentResult);
            Assert.NotNull(contentResult.Content);
        }

        [Fact]
        public async Task IfNotFoundReturnsNotFound()
        {
            IHttpActionResult actionResult = await _controller.GetDeviceMetadata("NOSUCHDEVICE");
            Assert.IsType<NotFoundResult>(actionResult);
        }
    }
}
