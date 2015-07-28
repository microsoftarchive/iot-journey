// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Practices.IoTJourney;
using Microsoft.Practices.IoTJourney.DeviceProvisioningModels;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Net;
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
        private static string ConnectionString = "";

        private static void Main(string[] args)
        {
            CommonConsoleHost.RunWithOptionsAsync(new Dictionary<string, Func<CancellationToken, Task>>
            {
                { "Register device", RegisterDeviceAsync },
                { "Provision device", ProvisionDeviceAsync },
                { "Send test message", SendTestMessage },
                { "Revoke device", RevokeDeviceAsync }
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

            Console.WriteLine("Registering device ID = {0}", DEVICE_ID);

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

            string urlPath = String.Format("api/devices/{0}/provision", DEVICE_ID);

            var result = await client.PostAsync(urlPath, null, token);
            Console.WriteLine("{0} ({1})", (int)result.StatusCode, result.ReasonPhrase);
            if (result.IsSuccessStatusCode)
            {
                var endpoint = await result.Content.ReadAsAsync<DeviceEndpoint>(token);

                Console.WriteLine("Endpoint: {0}", endpoint.Uri);
                Console.WriteLine("AccessToken: {0}", endpoint.AccessToken);

                ConnectionString = ServiceBusConnectionStringBuilder.CreateUsingSharedAccessSignature(
                                        new Uri(endpoint.Uri),
                                        endpoint.EventHubName,
                                        DEVICE_ID,
                                        endpoint.AccessToken
                                  );
            }
            else if (result.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("You must register the device first.");
            }
        }

        private static async Task RevokeDeviceAsync(CancellationToken token)
        {
            string baseUrl = ConfigurationHelper.GetConfigValue<string>("WebApiEndpoint");

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(baseUrl);

            string urlPath = String.Format("api/devices/{0}/revoke", DEVICE_ID);

            var result = await client.PostAsync(urlPath, null, token);
            Console.WriteLine("{0} ({1})", (int)result.StatusCode, result.ReasonPhrase);
        }

        private static Task SendTestMessage(CancellationToken token)
        {
            if (String.IsNullOrEmpty(ConnectionString))
            {
                Console.WriteLine("You must provision the device first.");
            }
            else
            {
                try
                {
                    var sender = EventHubSender.CreateFromConnectionString(ConnectionString);
                    sender.Send(new EventData(Encoding.UTF8.GetBytes("Hello Event Hub")));
                    Console.WriteLine("Wrote data to event hub");
                }
                catch (Microsoft.ServiceBus.Messaging.PublisherRevokedException)
                {
                    Console.WriteLine("The device was revoked. You must provision the device again.");
                }
            }
            return Task.FromResult(0);
        }
    }
}
