// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Microsoft.Practices.IoTJourney.Devices.Simulator.Tests
{
    public class WhenTrackingEventsToSend
    {
        [Fact]
        [Trait("Running time", "Short")]
        public void ShouldExpectToSendAfterElapsedTimePasses()
        {
            var frequency = TimeSpan.FromSeconds(1);
            var actualElapsed = TimeSpan.FromSeconds(2);

            var entry = new EventEntry(_ => null, frequency);

            entry.UpdateElapsedTime(actualElapsed);

            Assert.True(entry.ShouldSendEvent());
        }

        [Fact]
        [Trait("Running time", "Short")]
        public void ShouldTrackElapsedTimeAcrossMultipleUpdates()
        {
            const int FrequencyInSeconds = 2;

            var frequency = TimeSpan.FromSeconds(FrequencyInSeconds);
            var elapsed = TimeSpan.FromSeconds(FrequencyInSeconds / 2);

            var entry = new EventEntry(_ => null, frequency);

            // update 3 times, since 2 should not be enough to trigger
            entry.UpdateElapsedTime(elapsed);
            entry.UpdateElapsedTime(elapsed);
            entry.UpdateElapsedTime(elapsed);

            Assert.True(entry.ShouldSendEvent());
        }

        [Fact]
        [Trait("Running time", "Short")]
        public void ShouldNotExpectToSendIfElapsedTimeHasNotPassed()
        {
            var frequency = TimeSpan.FromSeconds(2);

            var elapsed = TimeSpan.FromSeconds(1);

            var entry = new EventEntry(_ => null, frequency);

            entry.UpdateElapsedTime(elapsed);

            Assert.False(entry.ShouldSendEvent());
        }

        [Fact]
        [Trait("Running time", "Short")]
        public void ShouldNotExpectToSendAfterReset()
        {
            var frequency = TimeSpan.FromSeconds(1);
            var actualElapsed = TimeSpan.FromSeconds(2);

            var entry = new EventEntry(_ => null, frequency);

            entry.UpdateElapsedTime(actualElapsed);

            Assert.True(entry.ShouldSendEvent());

            entry.ResetElapsedTime();

            Assert.False(entry.ShouldSendEvent());
        }
    }
}