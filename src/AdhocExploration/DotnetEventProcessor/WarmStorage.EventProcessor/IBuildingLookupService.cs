// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.WarmStorage
{
    public interface IBuildingLookupService
    {
        Task InitializeAsync();
        string GetBuildingId(string deviceId);
    }
}