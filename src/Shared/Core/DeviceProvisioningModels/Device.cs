// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Practices.IoTJourney.DeviceProvisioningModels
{
    public class Device
    {
        [Required]
        public string DeviceId { get; set; }
    }
}
