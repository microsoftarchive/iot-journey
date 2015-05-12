// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Practices.IoTJourney.Devices.Events;
using Xunit;

namespace Microsoft.Practices.IoTJourney.Devices.Simulator.Tests
{
    public class WhenSendingEvents
    {
        [Fact]
        [Trait("Running time", "Short")]
        public void EventTypeWillBeShortTypeName()
        {
            // This assumes that the event consumers are 
            // making the same choice.
            var evt = new UpdateTemperatureEvent();
            var expected = evt.GetType().Name;
            var actual = EventSender.DetermineTypeFromEvent(evt);

            Assert.Equal(expected, actual.Item1);
        }

        [Fact]
        [Trait("Running time", "Short")]
        public void EventTypeVersionAlwaysReturns1()
        {
            // TODO: The final system should not be hard coded
            var evt = new UpdateTemperatureEvent();

            var actual = EventSender.DetermineTypeFromEvent(evt);

            Assert.Equal(1, actual.Item2);
        }
    }
}