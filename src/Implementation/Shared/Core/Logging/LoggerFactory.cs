// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Practices.IoTJourney.Logging
{
    /// <summary>
    /// Generic pluggable logger factory for retrieving configured
    /// logging objects
    /// </summary>
    public abstract class LoggerFactory
    {
        #region "Singleton Implementation"

        private static ILogFactory _loggerFactory = null;

        private static object _lockObj = new object();

        protected static ILogFactory _factory
        {
            get
            {
                if (_loggerFactory == null)
                {
                    lock (_lockObj)
                    {
                        if (_loggerFactory == null)
                        {
                            _loggerFactory = new NoOpLoggerFactory();
                        }
                    }
                }
                return _loggerFactory;
            }
        }

        #endregion

        public static void Register(ILogFactory factory)
        {
            lock (_lockObj)
            {
                _loggerFactory = factory;
            }
        }

        public static void Initialize()
        {
            if (_loggerFactory != null)
                _loggerFactory.Initialize();
        }

        public static ILogger GetLogger<T>()
        {
            return _factory.Create(typeof(T).Name);
        }

        public static ILogger GetLogger()
        {
            return _factory.Create();
        }

        public static ILogger GetLogger(string logName)
        {
            return _factory.Create(logName);
        }
    }

    public interface ILogFactory
    {
        ILogger Create();
        ILogger Create(string name);
        void Initialize();
    }
}
