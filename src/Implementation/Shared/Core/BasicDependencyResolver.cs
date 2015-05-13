// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;

namespace Microsoft.Practices.IoTJourney
{
    public sealed class BasicDependencyResolver : IDependencyResolver
    {
        public IDependencyScope BeginScope()
        {
            return this;
        }

        public object GetService(Type serviceType)
        {
            return Activator.CreateInstance(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return new Object[] {Activator.CreateInstance(serviceType)};
        }

        public void Dispose()
        {
            
        }
    }
}
