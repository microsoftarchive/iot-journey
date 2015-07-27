// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SendEvents
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get values from configuration
            int numberOfDevices = int.Parse(ConfigurationManager.AppSettings["NumberOfDevices"]);
            string eventHubName = ConfigurationManager.AppSettings["EventHubName"];
            string eventHubNamespace = ConfigurationManager.AppSettings["EventHubNamespace"];
            string devicesSharedAccessPolicyName = ConfigurationManager.AppSettings["DevicesSharedAccessPolicyName"];
            string devicesSharedAccessPolicyKey = ConfigurationManager.AppSettings["DevicesSharedAccessPolicyKey"];

            string eventHubConnectionString = string.Format("Endpoint=sb://{0}.servicebus.windows.net/;SharedAccessKeyName={1};SharedAccessKey={2};TransportType=Amqp",
                eventHubNamespace, devicesSharedAccessPolicyName, devicesSharedAccessPolicyKey);

            var client = EventHubClient.CreateFromConnectionString(eventHubConnectionString, eventHubName);

            // Configure JSON to serialize properties using camelCase
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()};

            var random = new Random();
            
            try
            {
                Console.WriteLine("Sending messages to Event Hub {0}", client.Path);

                while (!Console.KeyAvailable)
                {
                    var tasks = new List<Task>();

                    // One event per device
                    for (int devices = 0; devices < numberOfDevices; devices++)
                    {
                        // Create the event
                        var info = new Event()
                        {
                            Id = devices.ToString(),
                            Lat = -30 + random.Next(75),
                            Lng = -120 + random.Next(70),
                            Time = DateTime.UtcNow.Ticks,
                            Code = (310 + random.Next(20)).ToString()
                        };

                        // Serialize to JSON
                        var serializedString = JsonConvert.SerializeObject(info);
                        Console.WriteLine(serializedString);
                        
                        // Create the message data
                        var bytes = Encoding.UTF8.GetBytes(serializedString);
                        var data = new EventData(bytes)
                        {
                            PartitionKey = info.Id
                        };

                        // Send the message to Event Hub
                        tasks.Add(client.SendAsync(data));
                    }

                    Task.WaitAll(tasks.ToArray());
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("Error on send: " + exp.Message);
            }
        }
    }
}
