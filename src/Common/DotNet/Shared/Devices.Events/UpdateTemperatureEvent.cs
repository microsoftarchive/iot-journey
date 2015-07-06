// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
namespace Microsoft.Practices.IoTJourney.Devices.Events
{
    public class UpdateTemperatureEvent
    {
        public string DeviceId { get; set; }

        /// <summary>
        /// The observation timestamp (device), UTC offset, stored as ticks 
        /// </summary>
        public DateTime TimeObserved { get; set; }

        /// <summary>
        /// Temperature reading in degrees Centigrade
        /// </summary>
        public float Temperature { get; set; }
    }
}