// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Practices.IoTJourney;
using Microsoft.Practices.IoTJourney.DeviceProvisioningModels;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonConsoleHost = Microsoft.Practices.IoTJourney.Tests.Common.ConsoleHost;

namespace ProvisioningWebApi.ConsoleHost
{
    internal class Program
    {
        private static string DEVICE_ID = Guid.NewGuid().ToString();

        private static void Main(string[] args)
        {
            CommonConsoleHost.RunWithOptionsAsync(new Dictionary<string, Func<CancellationToken, Task>>
            {
                { "Register a device", RegisterDeviceAsync },
                { "Provision a device", ProvisionDeviceAsync }
            }).Wait();
        }

        private static async Task RegisterDeviceAsync(CancellationToken token)
        {
            string baseUrl = ConfigurationHelper.GetConfigValue<string>("WebApiEndpoint");

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(baseUrl);

            var metadata = new DeviceMetadata
            {
                Building = 1,
                Room = 42
            };

            var result = await client.PutAsJsonAsync("api/registry/" + DEVICE_ID, metadata, token);
            Console.WriteLine("{0} ({1})", (int)result.StatusCode, result.ReasonPhrase);
            if (result.IsSuccessStatusCode)
            {
                var info = await result.Content.ReadAsAsync<DeviceInfo>(token);
                Console.WriteLine("Device state: {0}", info.Status);
            }
        }

        private static async Task ProvisionDeviceAsync(CancellationToken token)
        {
            string baseUrl = ConfigurationHelper.GetConfigValue<string>("WebApiEndpoint");

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(baseUrl);

            var device = new Device
            {
                DeviceId = DEVICE_ID
            };

            var result = await client.PostAsJsonAsync("api/provision", device, token);
            Console.WriteLine("{0} ({1})", (int)result.StatusCode, result.ReasonPhrase);
            if (result.IsSuccessStatusCode)
            {
                var endpoint = await result.Content.ReadAsAsync<DeviceEndpoint>(token);

                Console.WriteLine("Endpoint: {0}", endpoint.Uri);
                Console.WriteLine("AccessToken: {0}", endpoint.AccessToken);

                var connectionString = ServiceBusConnectionStringBuilder.CreateUsingSharedAccessSignature(
                                        new Uri(endpoint.Uri),
                                        endpoint.EventHubName,
                                        device.DeviceId,
                                        endpoint.AccessToken
                                  );

                var sender = EventHubSender.CreateFromConnectionString(connectionString);

                sender.Send(new EventData(Encoding.UTF8.GetBytes("Hello Event Hub")));

                Console.WriteLine("Wrote data to event hub");
            }
        }
    }
}
