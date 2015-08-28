using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor
{
    public class Configuration
    {
        public string EventHubNamespace { get; set; }
        public string EventHubName { get; set; }
        public string EventHubSasKeyName { get; set; }
        public string EventHubSasKey { get; set; }
        public string CheckpointStorageAccount { get; set; }

        public static Configuration GetCurrentConfiguration()
        {
            return new Configuration
            {
                EventHubNamespace = ConfigurationHelper.GetConfigValue<string>("EventHubNamespace"),
                EventHubName = ConfigurationHelper.GetConfigValue<string>("EventHubName"),
                EventHubSasKeyName = ConfigurationHelper.GetConfigValue<string>("EventHubSasKeyName"),
                EventHubSasKey = ConfigurationHelper.GetConfigValue<string>("EventHubSasKey"),
                CheckpointStorageAccount = ConfigurationHelper.GetConfigValue<string>("CheckpointStorageAccount")
            };
        }
    }
}
