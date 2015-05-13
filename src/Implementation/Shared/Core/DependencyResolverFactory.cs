// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Web.Http.Dependencies;

namespace Microsoft.Practices.IoTJourney
{
    public abstract class DependencyResolverFactory
    {
        private static IDependencyResolver _resolver = null;
        private static readonly object _lockObj = new object();

        protected static IDependencyResolver _factory
        {
            get
            {
                if (_resolver == null)
                {
                    lock (_lockObj)
                    {
                        if (_resolver == null)
                        {
                            _resolver = new BasicDependencyResolver();
                        }
                    }
                }
                return _resolver;
            }
        }

        public static void Register(IDependencyResolver resolver)
        {
            lock (_lockObj)
            {
                _resolver = resolver;
            }
        }
        public static IDependencyResolver GetResolver()
        {
            return _resolver;
        }
      
    }

    public static class DependencyExtensions
    {
        public static T GetService<T>(this IDependencyResolver resolver)
            where T : class
        {
            return ((IDependencyScope) resolver).GetService(typeof (T)) as T;
        }
    }
}
