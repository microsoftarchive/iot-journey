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
        /// If the task throws an exception, and it matches a given type
        /// that we want to ignore, then we emit an empty value to the 
        /// stream. Otherwise, propagate the original value.
        /// </summary>
        /// <typeparam name="T">The type of the task's result</typeparam>
        /// <param name="task">The task.</param>
        /// <param name="matches">A predicate identifying exceptions to ignore.</param>
        /// <returns>
        /// An observable that is empty if the exception matches, otherwise is 
        /// just the original value.
        /// </returns>
        public static IObservable<T> IgnoreCertainExcetions<T>(this Task<T> task, Predicate<Exception> matches)
        {
            return task
                .ToObservable()
                .Materialize()
                .SelectMany(notification => notification.Kind == NotificationKind.OnError && matches(notification.Exception)
                    ? Observable.Empty<T>()
                    : notification.ToObservable());
        }
    }
}