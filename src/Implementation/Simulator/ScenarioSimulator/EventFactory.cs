// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Practices.IoTJourney.Devices.Events;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator
{
    public static class EventFactory
    {
        public static UpdateTemperatureEvent TempuratureEventFactory(Random random)
        {
            return new UpdateTemperatureEvent
            {
                TimeStamp = DateTime.UtcNow.Ticks,
                Tempurature = random.Next(30),
            };
        }
    }
}