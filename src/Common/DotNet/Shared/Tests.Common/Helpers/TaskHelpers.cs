// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.Tests.Common.Helpers
{
    public static class TaskHelpers
    {
        public static Task CreateFaultedTask<TException>()
            where TException : Exception, new()
        {
            return CreateFaultedTask<object, TException>();
        }

        public static Task CreateFaultedTask<T, TException>()
            where TException : Exception, new()
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(new TException());
            return tcs.Task;
        }

        public static Task CreateCompletedTask()
        {
            return CreateCompletedTask(false);
        }

        public static Task<T> CreateCompletedTask<T>(T result)
        {
            return Task.FromResult(result);
        }
    }
}