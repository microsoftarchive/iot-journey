// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Practices.IoTJourney;
using Microsoft.Practices.IoTJourney.DeviceProvisioningModels;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.ProvisioningWebApi.DeviceRegistry
{
    public class TableStorageRegistry : IDeviceRegistry
    {
        const string TableName = "devices";

        // Translate betweeen DeviceInfo objects and table storage entities.
        class DeviceInfoEntity : TableEntity
        {
            public DeviceInfoEntity()
            {

            }
            public DeviceInfoEntity(DeviceInfo deviceInfo)
            {
                this.PartitionKey = deviceInfo.DeviceId;
                this.RowKey = deviceInfo.DeviceId;
                this.Status = deviceInfo.Status;
                if (deviceInfo.Metadata != null)
                {
                    this.Building = deviceInfo.Metadata.Building;
                    this.Room = deviceInfo.Metadata.Room;
                }
            }

            public string Status { get; set; }
            public int Building { get; set; }
            public int Room { get; set; }

            public DeviceInfo ToDeviceInfo()
            {
                return new DeviceInfo
                {
                    DeviceId = this.RowKey,
                    Metadata = new DeviceMetadata { Building = this.Building, Room = this.Room },
                    Status = this.Status
                };
            }

        }

        CloudTableClient _client;
        CloudTable _table;

        public TableStorageRegistry()
        {
            var storageConnectionString = ConfigurationHelper.GetConfigValue<string>("StorageConnectionString");
            var storageClient = CloudStorageAccount.Parse(storageConnectionString);
            _client = storageClient.CreateCloudTableClient();
            _table = _client.GetTableReference(TableName);
            _table.CreateIfNotExists();
        }

        public Task<bool> AddOrUpdateAsync(DeviceInfo info)
        {
            var entity = new DeviceInfoEntity(info);
            _table.Execute(TableOperation.InsertOrReplace(entity));
            return Task.FromResult(true);
        }

        public System.Threading.Tasks.Task<DeviceInfo> FindAsync(string id)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<DeviceInfoEntity>(id, id);
            TableResult retrievedResult = _table.Execute(retrieveOperation);

            var result = retrievedResult.Result as DeviceInfoEntity;

            DeviceInfo info = null;
            if (result != null)
            {
                info = result.ToDeviceInfo();
            }
            return Task.FromResult(info);
        }
    }
}
