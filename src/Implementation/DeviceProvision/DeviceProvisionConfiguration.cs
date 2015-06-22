using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.DeviceProvision
{
    public class DeviceProvisionConfiguration
    {
        public string EventHubNamespace {get; set;}

        public string EventHubName {get; set;}

        public string EventHubSasKeyName {get; set;}

        public string EventHubPrimaryKey {get; set;}

        public int EventHubTokenLifetimeDays {get; set;}


        public static DeviceProvisionConfiguration GetCurrentConfiguration()
        {
            return new DeviceProvisionConfiguration
            {
                EventHubNamespace = ConfigurationHelper.GetConfigValue<string>("DeviceProvision.EventHubNamespace"),
                EventHubName = ConfigurationHelper.GetConfigValue<string>("DeviceProvision.EventHubName"),
                EventHubSasKeyName = ConfigurationHelper.GetConfigValue<string>("DeviceProvision.EventHubSasKeyName"),
                EventHubPrimaryKey = ConfigurationHelper.GetConfigValue<string>("DeviceProvision.EventHubPrimaryKey"),
                EventHubTokenLifetimeDays = ConfigurationHelper.GetConfigValue<int>("DeviceProvision.EventHubTokenLifetimeDays")
            };
        }
    }
}
