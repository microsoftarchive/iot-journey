// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DeviceProvisioning.AccessTokens;
using DeviceProvisioning.DeviceRegistry;
using Microsoft.Practices.IoTJourney.DeviceProvisioningModels;
using System.Threading.Tasks;
using System.Web.Http;

namespace DeviceProvisioning.Controllers
{
    public class ProvisionController : ApiController
    {
        ITokenProvider _provisioner;
        IDeviceRegistry _registry;

        public ProvisionController(ITokenProvider provisioner, IDeviceRegistry registry)
        {
            _provisioner = provisioner;
            _registry = registry;
        }

        [HttpPost]
        public async Task<IHttpActionResult> ProvisionDevice([FromBody] Device device)
        {
            if (device == null)
            {
                return BadRequest("device is null");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var info = await _registry.FindAsync(device.DeviceId);
            if (info == null)
            {
                return NotFound();
            }

            var token = await _provisioner.GetTokenAsync(device.DeviceId);
            var endpoint = new DeviceEndpoint
            {
                Uri = _provisioner.EndpointUri.AbsoluteUri,
                EventHubName = _provisioner.EventHubName,
                AccessToken = token
            };

            // Update registry with new provisioning state
            info.Status = DeviceStateConstants.ProvisionedState;
            await _registry.AddOrUpdateAsync(info);

            return Ok(endpoint);
        }
    }
}
