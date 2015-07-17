// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DeviceProvisioning.DeviceRegistry;
using Microsoft.Practices.IoTJourney.DeviceProvisioningModels;
using System.Threading.Tasks;
using System.Web.Http;

namespace ProvisioningWebApi.Controllers
{
    public class RegistryController : ApiController
    {
        IDeviceRegistry _registry;

        public RegistryController(IDeviceRegistry registry)
        {
            _registry = registry;
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetDeviceMetadata(string id)
        {
            var info = await _registry.FindAsync(id);
            if (info == null)
            {
                return NotFound();
            }

            return Ok(info);
        }

        [HttpPut]
        public async Task<IHttpActionResult> PutDeviceMetadata(string id, [FromBody] DeviceMetadata metadata)
        {
            if (metadata == null)
            {
                return BadRequest("metadata is null");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            DeviceInfo info = await _registry.FindAsync(id);
            if (info == null)
            {
                info = new DeviceInfo 
                { 
                    DeviceId = id, 
                    Status = DeviceStateConstants.RegisteredState,
                    Metadata = metadata
                };
                await _registry.AddOrUpdateAsync(info);
                return CreatedAtRoute("DefaultApi", new { id = id }, info);
            }
            else
            {
                info.Metadata = metadata;
                await _registry.AddOrUpdateAsync(info);
                return Ok(info);
            }
        }
    }
}
