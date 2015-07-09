// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney
{
    /// <summary>
    /// Provides a pump that supports running asynchronous methods on the current thread.
    /// This is useful for creating an async context in a console application.
    /// </summary>
    /// <remarks>
    /// See http://blogs.msdn.com/b/pfxteam/archive/2012/01/20/10259049.aspx
    /// </remarks>
    public static class AsyncPump
    {
        /// <summary>
        /// Runs the specified asynchronous function.
        /// </summary>
        /// <param name="func">The asynchronous function to execute.</param>
        public static void Run(Func<Task> func)
        {
            if (func == null) throw new ArgumentNullException("func");

            var prevCtx = SynchronizationContext.Current;
            try
            {
                // Establish the new context
                var syncCtx = new SingleThreadSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(syncCtx);

                // Invoke the function and alert the context to when it completes
                var t = func();
                if (t == null) throw new InvalidOperationException("No task provided.");
                t.ContinueWith(delegate { syncCtx.Complete(); }, TaskScheduler.Default);

                // Pump continuations and propagate any exceptions
                syncCtx.RunOnCurrentThread();
                t.GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevCtx);
            }
        }

        /// <summary>
        /// Provides a SynchronizationContext that's single-threaded.
        /// </summary>
        private sealed class SingleThreadSynchronizationContext : SynchronizationContext
        {
            /// <summary>
            /// The queue of work items.
            /// </summary>
            private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> _queue =
                new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();

            /// <summary>
            /// Dispatches an asynchronous message to the synchronization context.
            /// </summary>
            /// <param name="d">The System.Threading.SendOrPostCallback delegate to call.</param>
            /// <param name="state">The object passed to the delegate.</param>
            public override void Post(SendOrPostCallback d, object state)
            {
                if (d == null) throw new ArgumentNullException("d");

                _queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
            }

            /// <summary>
            /// Not supported.
            /// </summary>
            public override void Send(SendOrPostCallback d, object state)
            {
                throw new NotSupportedException("Synchronously sending is not supported.");
            }

            /// <summary>
            /// Runs an loop to process all queued work items.
            /// </summary>
            public void RunOnCurrentThread()
            {
                foreach (var workItem in _queue.GetConsumingEnumerable())
                    workItem.Key(workItem.Value);
            }

            /// <summary>
            /// Notifies the context that no more work will arrive.
            /// </summary>
            public void Complete() { _queue.CompleteAdding(); }
        }
    }
}