// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Practices.IoTJourney.DeviceProvisioningModels
{
    public class DeviceInfo
    {
        public string DeviceId { get; set; }
        public string Status { get; set; }
        public DeviceMetadata Metadata { get; set; }
    }
}
