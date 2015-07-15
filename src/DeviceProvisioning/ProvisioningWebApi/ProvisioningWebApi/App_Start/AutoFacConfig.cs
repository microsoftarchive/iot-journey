// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Autofac;
using Autofac.Integration.WebApi;
using DeviceProvisioning.AccessTokens;
using DeviceProvisioning.DeviceRegistry;
using System.Reflection;
using System.Web.Http;

namespace DeviceProvisioning
{
    public static class AutoFacConfig
    {
        public static void Configure()
        {
            var builder = new ContainerBuilder();
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            builder.RegisterInstance<ITokenProvider>(new TokenProvider());
            builder.RegisterInstance<IDeviceRegistry>(new TableStorageRegistry());

            // Set the dependency resolver to be Autofac.
            var container = builder.Build();
            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}
