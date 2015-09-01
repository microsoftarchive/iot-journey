// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Practices.IoTJourney;
using Microsoft.ServiceBus;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace DeviceProvisioning.AccessTokens
{
    public class TokenProvider : ITokenProvider
    {
        private string EventHubPrimaryKey;
        private string EventHubNamespace;
        private string EventHubSasKeyName;
        private string EventHubConnectionString;
        private double EventHubTokenLifetimeDays = 10;

        public TokenProvider()
        {
            EventHubPrimaryKey = ConfigurationHelper.GetConfigValue<string>("EventHubPrimaryKey");
            EventHubNamespace = ConfigurationHelper.GetConfigValue<string>("EventHubNamespace");
            EventHubName = ConfigurationHelper.GetConfigValue<string>("EventHubName");
            EventHubSasKeyName = ConfigurationHelper.GetConfigValue<string>("EventHubSasKeyName");
            EventHubConnectionString = ConfigurationHelper.GetConfigValue<string>("EventHubConnectionString");
            EndpointUri = new Uri(string.Format(CultureInfo.InvariantCulture,
                "https://{0}.servicebus.windows.net", 
                EventHubNamespace));
        }

        public Uri EndpointUri { get; private set; }

        public string EventHubName { get; private set; }

        public Task<string> GetTokenAsync(string DeviceId)
        {
            var endpoint = ServiceBusEnvironment.CreateServiceUri("sb", EventHubNamespace, string.Empty);

            // Generate token for the device.
            string deviceToken = SharedAccessSignatureTokenProvider.GetPublisherSharedAccessSignature
            (
                endpoint,
                EventHubName,
                DeviceId,
                EventHubSasKeyName,
                EventHubPrimaryKey,
                TimeSpan.FromDays(EventHubTokenLifetimeDays)
            );

            return Task.FromResult(deviceToken);
        }

        public Task RevokeDeviceAsync(string DeviceId)
        {
            var nsm = NamespaceManager.CreateFromConnectionString(EventHubConnectionString);
            return nsm.RevokePublisherAsync(EventHubName, DeviceId);
        }

        public Task RestoreDeviceAsync(string DeviceId)
        {
            var nsm = NamespaceManager.CreateFromConnectionString(EventHubConnectionString);
            return nsm.RestorePublisherAsync(EventHubName, DeviceId);
        }
    }
}
