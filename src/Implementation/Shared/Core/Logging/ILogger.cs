// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Practices.IoTJourney.Logging
{
    #region "Usings"

    

    #endregion

    /// <summary>
    /// Definition of a generic logging interface to abstract 
    /// implementation details.  Includes api trace methods.
    /// </summary>
    public interface ILogger
    {       
        void Info(object message);
        void Info(string fmt, params object[] vars);
        void Info(Exception exception, string fmt, params object[] vars);

        void Info(Guid activityId, object message);
        void Info(Guid activityId, string fmt, params object[] vars);
        void Info(Guid activityId, Exception exception, string fmt, params object[] vars);

        void Debug(object message);
        void Debug(string fmt, params object[] vars);
        void Debug(Exception exception, string fmt, params object[] vars);

        void Debug(Guid activityId, object message);
        void Debug(Guid activityId, string fmt, params object[] vars);
        void Debug(Guid activityId, Exception exception, string fmt, params object[] vars);

        void Warning(object message);
        void Warning(string fmt, params object[] vars);
        void Warning(Exception exception, string fmt, params object[] vars);

        void Warning(Guid activityId, object message);
        void Warning(Guid activityId, string fmt, params object[] vars);
        void Warning(Guid activityId, Exception exception, string fmt, params object[] vars);

        void Error(object message);
        void Error(string fmt, params object[] vars);
        void Error(Exception exception, string fmt, params object[] vars);

        void Error(Guid activityId, object message);
        void Error(Guid activityId, string fmt, params object[] vars);
        void Error(Guid activityId, Exception exception, string fmt, params object[] vars);
     
        Guid TraceIn(string method);
        Guid TraceIn(string method, string properties);
        Guid TraceIn(string method, string fmt, params object[] vars);

        void TraceIn(Guid activityId, string method);
        void TraceIn(Guid activityId, string method, string properties);
        void TraceIn(Guid activityId, string method, string fmt, params object[] vars);

        void TraceOut(Guid activityId, string method);
        void TraceOut(Guid activityId, string method, string properties);
        void TraceOut(Guid activityId, string method, string fmt, params object[] vars);

        void TraceApi(string method, TimeSpan timespan);
        void TraceApi(string method, TimeSpan timespan, string properties);
        void TraceApi(string method, TimeSpan timespan, string fmt, params object[] vars);        
    }
}
