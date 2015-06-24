// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Practices.IoTJourney.Tests.Common
{
    public static class AssertExt
    {
        public static async Task ThrowsAsync<T>(Func<Task> action)
            where T : Exception
        {
            try
            {
                await action();
                Assert.False(true, string.Format("Did not throw the expected exception of type {0}", typeof(T).Name));
            }
            catch (T)
            {
                return;
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Did not throw the expected exception of type {0}. Instead threw {1}.", typeof(T).Name, e.ToString()));
            }
        }

        public async static Task CompletesBeforeTimeoutAsync(Task task, TimeSpan timeout)
        {
            await Task.WhenAny(task, Task.Delay(timeout));
            Assert.True(task.Status == TaskStatus.RanToCompletion);
        }

        public async static Task DoesNotCompleteBeforeTimeoutAsync(Task task, TimeSpan timeout)
        {
            await Task.WhenAny(task, Task.Delay(timeout));
            Assert.True(task.Status != TaskStatus.RanToCompletion);
        }

        public static async Task TaskRanForAtLeast(Func<Task> launchTask, TimeSpan duration)
        {
            var sw = new Stopwatch();
            sw.Start();
            await launchTask();

            Assert.True(
                sw.Elapsed > duration,
                string.Format("The task was expected to run for at least {0} but only ran for {1}", duration, sw.Elapsed)
                );
        }
    }
}