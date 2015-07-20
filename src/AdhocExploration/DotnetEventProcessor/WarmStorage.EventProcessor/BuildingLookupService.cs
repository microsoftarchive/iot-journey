// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace Microsoft.Practices.IoTJourney.WarmStorage
{
    public class BuildingLookupService : IBuildingLookupService
    {
        private static ConcurrentDictionary<string, string> cachedBuildingDictionary = new ConcurrentDictionary<string, string>();
        private static string blobETag;
        private static CloudBlockBlob blockBlob;
        private static Configuration configuration;
        private static Timer timer;

        static BuildingLookupService()
        {
            configuration = Configuration.GetCurrentConfiguration();
        }

        public async Task InitializeAsync()
        {
            await CheckForUpdateAsync();

            timer = new Timer((s) => CheckForUpdateAsync(), null, TimeSpan.FromMinutes(configuration.ReferenceDataCacheTTLMinutes),
                TimeSpan.FromMinutes(configuration.ReferenceDataCacheTTLMinutes));
        }

        public string GetBuildingId(string deviceId)
        {
            string buildingId;
            cachedBuildingDictionary.TryGetValue(deviceId, out buildingId);
            return buildingId;
        }

        private static async Task CheckForUpdateAsync()
        {
            var storageAccount = CloudStorageAccount.Parse(configuration.ReferenceDataStorageAccount);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(configuration.ReferenceDataStorageContainer);
            blockBlob = container.GetBlockBlobReference(configuration.ReferenceDataFilePath);

            await blockBlob.FetchAttributesAsync();

            if (blockBlob.Properties.ETag == blobETag)
            {
                return;
            }

            await LoadDictionaryAsync();
            blobETag = blockBlob.Properties.ETag;
        }

        private static async Task LoadDictionaryAsync()
        {
            string text;
            using (var memoryStream = new MemoryStream())
            {
                await blockBlob.DownloadToStreamAsync(memoryStream);
                text = Encoding.UTF8.GetString(memoryStream.ToArray());
            }

            var buildingMappings = JsonConvert.DeserializeObject<BuildingMapping[]>(text);

            foreach (var buildingMapping in buildingMappings)
            {
                cachedBuildingDictionary[buildingMapping.DeviceId] = buildingMapping.BuildingId;
            }
        }
    }
}