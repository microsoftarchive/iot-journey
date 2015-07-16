// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator.Tests
{
    public class WhenTrackingEventsToSend
    {
        [Fact]
        [Trait("Running time", "Short")]
        public void ShouldExpectToSendAfterElapsedTimePasses()
        {
            var frequency = TimeSpan.FromSeconds(1);
            var actualElapsed = TimeSpan.FromSeconds(2);

            var entry = new EventEntry((r, d) => null, frequency);

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

            var entry = new EventEntry((r, d) => null, frequency);

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

            var entry = new EventEntry((r, d) => null, frequency);

            entry.UpdateElapsedTime(elapsed);

            Assert.False(entry.ShouldSendEvent());
        }

        [Fact]
        [Trait("Running time", "Short")]
        public void ShouldNotExpectToSendAfterReset()
        {
            var frequency = TimeSpan.FromSeconds(1);
            var elapsed = frequency.Add(TimeSpan.FromMilliseconds(100));

            var entry = new EventEntry((r, d) => null, frequency);

            entry.UpdateElapsedTime(elapsed);
            Assert.True(entry.ShouldSendEvent());

            entry.ResetElapsedTime();
            Assert.False(entry.ShouldSendEvent());
        }

        [Fact]
        [Trait("Running time", "Short")]
        public void ShouldPreserveRemainingTimeWhenResetting()
        {
            var eventFrequency = TimeSpan.FromSeconds(1);
            var excessTime = TimeSpan.FromMilliseconds(250);
            var entry = new EventEntry((r, d) => null, eventFrequency);
            entry.UpdateElapsedTime(eventFrequency.Add(excessTime));
            entry.ResetElapsedTime();

            Assert.Equal(excessTime, entry.ElapsedTime);
        }

        [Fact]
        [Trait("Sending with Jitter", "short")]
        public void JitterShouldOscillateAroundSendingFrequency()
        {
            const int numberOfTests = 10000;
            var frequency = TimeSpan.FromSeconds(1);

            const double percentToJitter = 0.1;
            var jitterDelta = TimeSpan.FromMilliseconds(frequency.TotalMilliseconds * percentToJitter);
            var maxFrequency = frequency.Add(jitterDelta);
            var minFrequency = frequency.Subtract(jitterDelta);

            var entry = new EventEntry((r, d) => null, frequency, percentToJitter);
            var jitterValues = new List<double>();

            // Working with a bounded random value, validate the following
            // - Validate it doesn't go past max or min boundary.
            // - Validate we are statistically close to our center freq value. This reflects we have seen both +/- values 

            int countOverMax = 0;
            int countUnderMin = 0;

            for (int i = 0; i < numberOfTests; i++)
            {
                entry.ResetElapsedTime(); //reseting changes the FrequencyWithJitter
                var jitter = entry.FrequencyWithJitter;
                jitterValues.Add(jitter.TotalMilliseconds);

                if (jitter > maxFrequency)
                {
                    countOverMax++;
                }
                if (jitter < minFrequency)
                {
                    countUnderMin++;
                }
            }

            Assert.True(countOverMax == 0, "Frequency jitter exceeded upper boundary");
            Assert.True(countUnderMin == 0, "Frequency jitter exceeded lower boundary");

            // This is an indicator of how the accurate our elasped time is with the frequency
            const double percentAccurancy = 0.99;
            var maxPerFreq = frequency.TotalMilliseconds * (2 - percentAccurancy);
            var minPerFreq = frequency.TotalMilliseconds * percentAccurancy;

            // Validate that it should be statistically close to 100% of the original frequency. 
            var avgValue = (jitterValues.Sum() / numberOfTests);
            Assert.InRange(avgValue, minPerFreq, maxPerFreq);
        }
    }
}