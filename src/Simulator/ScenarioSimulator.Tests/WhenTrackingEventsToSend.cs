// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
            var actualElapsed = TimeSpan.FromSeconds(2);

            var entry = new EventEntry((r, d) => null, frequency);

            entry.UpdateElapsedTime(actualElapsed);

            Assert.True(entry.ShouldSendEvent());

            entry.ResetElapsedTime();

            Assert.False(entry.ShouldSendEvent());
        }

        [Fact]
        [Trait("Sending with Jitter","short")]
        public void JitterShouldOscillateAroundSendingFrequency()
        {
            const int numberOfTest = 10000;
            var frequency = TimeSpan.FromSeconds(1);
            var elasped = TimeSpan.FromSeconds(0.01);
            var percentToJitter = 0.1;
            var maxFrequency = frequency.TotalMilliseconds + (frequency.TotalMilliseconds * percentToJitter);
            var minFrequency = frequency.TotalMilliseconds - (frequency.TotalMilliseconds * percentToJitter);
            var testMinFreq = frequency.TotalMilliseconds;
            var testMaxFreq = frequency.TotalMilliseconds;

            var entry = new EventEntry((r, d) => null, frequency, percentToJitter);
            double[] testArray = new double[numberOfTest];

            bool upperFlag = false;
            bool lowerFlag = false;
            // Working with a bounded random value, validate the following
            // - Validate it doesn't go past max or min boundary.
            // - Validate we are statistically close to our center freq value. This reflects we have seen both +/- values 
            for (int i = 0; i < numberOfTest; i++)
            {
                entry.ResetElapsedTime();
                do
                {
                    entry.UpdateElapsedTime(elasped);
                } while (!entry.ShouldSendEvent());

                var timeElapsed = entry.ElapsedTime.TotalMilliseconds;
                testArray[i] = timeElapsed;
                // validate it's below & above frequency at times
                if (timeElapsed > maxFrequency)
                {
                    upperFlag = true;
                }
                if (timeElapsed < minFrequency)
                {
                    lowerFlag = true;
                }
                if (timeElapsed > testMaxFreq)
                {
                    testMaxFreq = timeElapsed;
                }
                if (timeElapsed < testMinFreq)
                {
                    testMinFreq = timeElapsed;
                }


            }

            Assert.False(upperFlag, "Frequency jitter exceeded upper boundary");
            Assert.False(lowerFlag, "Frequency jitter exceeded lower boundary");
            var avgValue = (testArray.Sum() / numberOfTest) ;
            //Assert.True(testMinFreq != frequency.TotalMilliseconds,"Frequency never generated a minimum frequency");
            Assert.True(testMaxFreq != frequency.TotalMilliseconds, "Frequency never generated a maximum frequency");
            var percentageAccurate = (avgValue/ frequency.TotalMilliseconds) * 100;

            // VAlidate that it should be statistically close to 100% of the original frequency.  
            Assert.InRange(percentageAccurate, 98.5, 101.5);



        }
    }
}