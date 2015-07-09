// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Practices.IoTJourney.DeviceProvisioningModels;
using System.Threading.Tasks;

namespace DeviceProvisioning.DeviceRegistry
{
    public interface IDeviceRegistry
    {
        Task<bool> AddOrUpdate(DeviceInfo info);
        Task<DeviceInfo> Find(string id);
    }
}
