// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator
{
    public static class Serializer
    {
        public static byte[] ToJsonUTF8(object content)
        {
            var json = JsonConvert.SerializeObject(content);

            return Encoding.UTF8.GetBytes(json);
        }
    }
}