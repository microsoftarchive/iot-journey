// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DeviceProvisioning.AccessTokens;
using DeviceProvisioning.DeviceRegistry;
using Microsoft.Practices.IoTJourney.DeviceProvisioningModels;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace DeviceProvisioning.Controllers
{
    [RoutePrefix("api/devices")]
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
        [Route("{deviceId}/provision")]
        public async Task<IHttpActionResult> ProvisionDevice(string deviceId)
        {
            var info = await _registry.FindAsync(deviceId);
            if (info == null)
            {
                return NotFound();
            }

            // If the device was revoked, restore it.
            if (info.Status.Equals(DeviceStateConstants.RevokedState, StringComparison.Ordinal))
            {
                await _provisioner.RestoreDeviceAsync(deviceId);
            }

            var token = await _provisioner.GetTokenAsync(deviceId);
            var endpoint = new DeviceEndpoint
            {
                Uri = _provisioner.EndpointUri.AbsoluteUri,
                EventHubName = _provisioner.EventHubName,
                AccessToken = token
            };

            // Update registry with new provisioning state.
            info.Status = DeviceStateConstants.ProvisionedState;
            await _registry.AddOrUpdateAsync(info);

            return Ok(endpoint);
        }

        [HttpPost]
        [Route("{deviceId}/revoke")]
        public async Task<IHttpActionResult> RevokeDevice(string deviceId)
        {
            var info = await _registry.FindAsync(deviceId);
            if (info == null)
            {
                return NotFound();
            }

            await _provisioner.RevokeDeviceAsync(deviceId);

            // Update registry with new provisioning state.
            info.Status = DeviceStateConstants.RevokedState;
            await _registry.AddOrUpdateAsync(info);

            return Ok(info);
        }
    }
}
