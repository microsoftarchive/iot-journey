// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace Microsoft.Practices.IoTJourney.WarmStorage
{
    public class BuildingLookupService : IBuildingLookupService
    {
        private static ConcurrentDictionary<string, string> cachedBuildingDictionary;
        private static string blobETag;
        private static CloudBlockBlob blockBlob;
        private static bool isGettingLatestETag, isLoadingDictionary;
        private static Task gettingLatestETagTask, loadingTask;
        private static DateTimeOffset cacheDateTimeOffset;
        private static Configuration configuration;

        static BuildingLookupService()
        {
            configuration = Configuration.GetCurrentConfiguration();
            CheckForUpdateAsync().Wait();
        }
        public async Task<string> GetBuildingIdAsync(string deviceId)
        {
            await CheckForUpdateAsync();

            string buildingId;
            cachedBuildingDictionary.TryGetValue(deviceId, out buildingId);
            return buildingId;
        }

        private static async Task CheckForUpdateAsync()
        {
            if (cacheDateTimeOffset != null &&
                DateTimeOffset.Compare(cacheDateTimeOffset.AddMinutes(configuration.ReferenceDataCacheTTLMinutes), DateTimeOffset.UtcNow) > 0) return;

            //If currently fetching ETag, assume cache is still valid.
            if (isGettingLatestETag) return;

            isGettingLatestETag = true;
            var storageAccount = CloudStorageAccount.Parse(configuration.ReferenceDataStorageAccount);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(configuration.ReferenceDataStorageContainer);
            blockBlob = container.GetBlockBlobReference(configuration.ReferenceDataFilePath);

            gettingLatestETagTask = blockBlob.FetchAttributesAsync();
            await gettingLatestETagTask;
            isGettingLatestETag = false;

            if (cachedBuildingDictionary != null && blockBlob.Properties.ETag == blobETag)
            {
                cacheDateTimeOffset = DateTimeOffset.UtcNow;
                return;
            }

            if (isLoadingDictionary)
            {
                await loadingTask;
            }
            else
            {
                isLoadingDictionary = true;
                loadingTask = LoadDictionaryAsync();
                await loadingTask;
                blobETag = blockBlob.Properties.ETag;
                isLoadingDictionary = false;
                cacheDateTimeOffset = DateTimeOffset.UtcNow;
            }

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
            cachedBuildingDictionary = new ConcurrentDictionary<string, string>();
            foreach (var buildingMapping in buildingMappings)
            {
                cachedBuildingDictionary[buildingMapping.DeviceId] = buildingMapping.BuildingId;
            }
        }
    }
}