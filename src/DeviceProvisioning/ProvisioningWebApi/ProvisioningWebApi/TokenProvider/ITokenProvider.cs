// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace DeviceProvisioning.AccessTokens
{
    public interface ITokenProvider
    {
        Uri EndpointUri { get; }
        string EventHubName { get; }
        Task<string> GetTokenAsync(string DeviceId);
        Task RevokeDeviceAsync(string DeviceId);
        Task RestoreDeviceAsync(string DeviceId);
    }
}
