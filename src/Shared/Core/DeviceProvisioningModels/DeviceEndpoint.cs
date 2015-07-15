// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Practices.IoTJourney.DeviceProvisioningModels
{
    public class DeviceEndpoint
    {
        public string Uri { get; set; }
        public string EventHubName { get; set; }
        public string AccessToken { get; set; }
    }
}
