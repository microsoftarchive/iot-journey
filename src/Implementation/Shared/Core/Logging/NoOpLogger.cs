// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Practices.IoTJourney.Logging
{
    public class NoOpLogger : ILogger
    {
        public void Info(object message)
        {
        }

        public void Info(string fmt, params object[] vars)
        {
        }

        public void Info(Exception exception, string fmt, params object[] vars)
        {
        }

        public void Info(Guid activityId, object message)
        {
        }

        public void Info(Guid activityId, string fmt, params object[] vars)
        {
        }

        public void Info(Guid activityId, Exception exception, string fmt, params object[] vars)
        {
        }

        public void Debug(object message)
        {
        }

        public void Debug(string fmt, params object[] vars)
        {
        }

        public void Debug(Exception exception, string fmt, params object[] vars)
        {
        }

        public void Debug(Guid activityId, object message)
        {
        }

        public void Debug(Guid activityId, string fmt, params object[] vars)
        {
        }

        public void Debug(Guid activityId, Exception exception, string fmt, params object[] vars)
        {
        }

        public void Warning(object message)
        {
        }

        public void Warning(string fmt, params object[] vars)
        {
        }

        public void Warning(Exception exception, string fmt, params object[] vars)
        {
        }

        public void Warning(Guid activityId, object message)
        {
        }

        public void Warning(Guid activityId, string fmt, params object[] vars)
        {
        }

        public void Warning(Guid activityId, Exception exception, string fmt, params object[] vars)
        {
        }

        public void Error(object message)
        {
        }

        public void Error(string fmt, params object[] vars)
        {
        }

        public void Error(Exception exception, string fmt, params object[] vars)
        {
        }

        public void Error(Guid activityId, object message)
        {
        }

        public void Error(Guid activityId, string fmt, params object[] vars)
        {
        }

        public void Error(Guid activityId, Exception exception, string fmt, params object[] vars)
        {
        }

        public Guid TraceIn(string method)
        {
            return Guid.Empty;
        }

        public Guid TraceIn(string method, string properties)
        {
            return Guid.Empty;
        }

        public Guid TraceIn(string method, string fmt, params object[] vars)
        {
            return Guid.Empty;
        }

        public void TraceIn(Guid activityId, string method)
        {
        }

        public void TraceIn(Guid activityId, string method, string properties)
        {
        }

        public void TraceIn(Guid activityId, string method, string fmt, params object[] vars)
        {
        }

        public void TraceOut(Guid activityId, string method)
        {
        }

        public void TraceOut(Guid activityId, string method, string properties)
        {
        }

        public void TraceOut(Guid activityId, string method, string fmt, params object[] vars)
        {
        }

        public void TraceApi(string method, TimeSpan timespan)
        {
        }

        public void TraceApi(string method, TimeSpan timespan, string properties)
        {
        }

        public void TraceApi(string method, TimeSpan timespan, string fmt, params object[] vars)
        {
        }
    }
}
