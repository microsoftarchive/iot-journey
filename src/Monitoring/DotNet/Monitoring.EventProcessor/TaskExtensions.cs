using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor
{
    public static class TaskExtensions
    {
        /// <summary>
        /// If the task throws a timeout exception, we want to emit
        /// an empty value to the stream. Otherwise, propagate the 
        /// original value.
        /// </summary>
        /// <typeparam name="T">The type of the task's result</typeparam>
        /// <param name="task">The task.</param>
        /// <returns></returns>
        public static IObservable<T> IgnoreTimeouts<T>(this Task<T> task)
        {
            return task
                .ToObservable()
                .Materialize()
                .SelectMany(notification => IsTimeoutException(notification)
                    ? Observable.Empty<T>()
                    : notification.ToObservable());
        }

        private static bool IsTimeoutException<T>(Notification<T> notification)
        {
            return notification.Kind == NotificationKind.OnError
                   && notification.Exception is TimeoutException;
        }
    }
}