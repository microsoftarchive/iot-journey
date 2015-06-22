using Microsoft.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.DeviceProvision
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AsyncPump.Run(() => MainAsync(args));
        }

        // In-memory registry where we save device provision info.
        private static IDictionary<string, DeviceInfo> _registry = new Dictionary<string, DeviceInfo>();

        private static async Task MainAsync(string[] args)
        {
            await ProvisionAsync();
        }

        private static async Task ProvisionAsync()
        {
            var devices = new List<DeviceInfo>();
            using(var reader = new StreamReader("fabrikam_buildingdevice.json"))
            {
                var json = await reader.ReadToEndAsync();
                devices = JsonConvert.DeserializeObject<List<DeviceInfo>>(json);
            }

            var configuration = DeviceProvisionConfiguration.GetCurrentConfiguration();

            foreach(var device in devices)
            {
                device.Token = SharedAccessSignatureTokenProvider.GetPublisherSharedAccessSignature(
                       ServiceBusEnvironment.CreateServiceUri("sb", configuration.EventHubNamespace, string.Empty),
                       configuration.EventHubName,
                       device.DeviceId,
                       configuration.EventHubSasKeyName,
                       configuration.EventHubPrimaryKey,
                       TimeSpan.FromDays(configuration.EventHubTokenLifetimeDays)
                );

                _registry.Add(device.DeviceId, device);
            }
        }
    }
}
