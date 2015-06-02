// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Practices.IoTJourney.ColdStorage
{
    public class ColdStorageEvent
    {
        public Dictionary<string, string> Properties { get; set; }
        public string Offset { get; set; }
        public dynamic Payload { get; set; }
    }
}