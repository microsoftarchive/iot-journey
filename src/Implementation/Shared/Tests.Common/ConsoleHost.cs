// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.Tests.Common
{
    public static class ConsoleHost
    {
        public static void WithOptions(Dictionary<string, Func<CancellationToken, Task>> actions)
        {
            WithOptions(actions, Timeout.InfiniteTimeSpan);
        }

        public static void WithOptions(Dictionary<string, Func<CancellationToken, Task>> actions, TimeSpan timeout)
        {
            actions.Add("Exit", token => { Environment.Exit(0); return default(Task); });

            while (true)
            {
                var tokenSource = new CancellationTokenSource(timeout);

                using (Color(ConsoleColor.Yellow))
                {
                    Console.WriteLine();
                    Console.WriteLine(Assembly.GetEntryAssembly().GetName().Name);
                    Console.WriteLine();
                }

                actions.Keys.Select((title, index) => new { title, index })
                    .ToList()
                    .ForEach(t => Console.WriteLine("[{0}] {1}", t.index + 1, t.title));

                Console.WriteLine();
                Console.Write("Select an option: ");

                var key = Console.ReadKey().KeyChar.ToString(CultureInfo.InvariantCulture);
                Console.WriteLine();

                int option = 1;
                if (!int.TryParse(key, out option))
                {
                    Environment.Exit(0);
                }

                option--;

                var selection = actions.ToList()[option];

                Console.Write("executing ");
                using (Color(ConsoleColor.Green))
                {
                    Console.WriteLine(selection.Key);
                }

                var running = selection
                    .Value(tokenSource.Token)
                    .ContinueWith(ReportTaskStatus);

                using (Color(ConsoleColor.DarkGreen))
                {
                    Console.WriteLine("press `q` to signal termination");
                }

                var input = Console.ReadKey();
                if (input.KeyChar == 'q')
                {
                    using (Color(ConsoleColor.DarkGreen))
                    {
                        Console.WriteLine();
                        Console.WriteLine("termination signal sent");
                    }
                    tokenSource.Cancel();
                }

                running.Wait();

                Console.ReadKey();
                Console.Clear();
            }
        }

        private static void ReportTaskStatus(Task task)
        {
            if (task.IsFaulted)
            {
                using (Color(ConsoleColor.Red))
                {
                    Console.WriteLine("an exception occurred");
                }
                Console.WriteLine(task.Exception);
            }
            else if (task.IsCanceled)
            {
                using (Color(ConsoleColor.DarkYellow))
                {
                    Console.WriteLine("cancelled");
                }
            }
            else
            {
                using (Color(ConsoleColor.Blue))
                {
                    Console.WriteLine("completed successfully");
                }
            }

            using (Color(ConsoleColor.DarkGreen))
            {
                Console.WriteLine("press any key to return to the menu");
            }
        }

        public static IDisposable Color(ConsoleColor color)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = color;

            return Disposable.Create(
                () => Console.ForegroundColor = original
                );
        }

        public static class Disposable
        {
            public static IDisposable Create(Action whenDisposingAction)
            {
                return new ActionOnDispose(whenDisposingAction);
            }

            private class ActionOnDispose : IDisposable
            {
                private readonly Action _action;

                internal ActionOnDispose(Action action)
                {
                    _action = action;
                }

                public void Dispose()
                {
                    _action();
                }
            }
        }
    }
}