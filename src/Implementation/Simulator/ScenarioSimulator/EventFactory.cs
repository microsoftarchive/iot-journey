// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Practices.IoTJourney.Devices.Events;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator
{
    public static class EventFactory
    {
        public static UpdateTemperatureEvent TemperatureEventFactory(Random random, Device device)
        {
            if (!device.CurrentTemperature.HasValue)
            {
                device.CurrentTemperature = random.Next(25);
            }
            else
            {
                var temperatureChange = random.Next(-2,3);
                device.CurrentTemperature += temperatureChange;
            }

            return new UpdateTemperatureEvent
            {
                DeviceId = device.Id,
                TimeStamp = DateTime.UtcNow.Ticks,
                Temperature = device.CurrentTemperature.Value,
            };
        }

        public static UpdateTemperatureEvent ThirtyDegreeTemperatureEventFactory(Random random, Device device)
        {
            return new UpdateTemperatureEvent
            {
                DeviceId = device.Id,
                TimeStamp = DateTime.UtcNow.Ticks,
                Temperature = 30,
            };
        }
    }
}