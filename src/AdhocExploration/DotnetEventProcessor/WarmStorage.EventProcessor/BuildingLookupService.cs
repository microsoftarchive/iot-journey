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

namespace Microsoft.Practices.IoTJourney.WarmStorage.EventProcessor
{
    public class BuildingLookupService : IBuildingLookupService
    {
        private readonly ConcurrentDictionary<string, string> _cachedBuildingDictionary = new ConcurrentDictionary<string, string>();
        private readonly Configuration _configuration;
        private string _blobETag;
        private CloudBlockBlob _blockBlob;
        private Timer _timer;

        public BuildingLookupService()
        {
            _configuration = Configuration.GetCurrentConfiguration();
        }

        public async Task InitializeAsync()
        {
            await CheckForUpdateAsync();

            _timer = new Timer((s) => CheckForUpdateAsync(), null, TimeSpan.FromMinutes(_configuration.ReferenceDataCacheTTLMinutes),
                TimeSpan.FromMinutes(_configuration.ReferenceDataCacheTTLMinutes));
        }

        public string GetBuildingId(string deviceId)
        {
            string buildingId;
            _cachedBuildingDictionary.TryGetValue(deviceId, out buildingId);
            return buildingId;
        }

        private async Task CheckForUpdateAsync()
        {
            var storageAccount = CloudStorageAccount.Parse(_configuration.ReferenceDataStorageAccount);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(_configuration.ReferenceDataStorageContainer);
            _blockBlob = container.GetBlockBlobReference(_configuration.ReferenceDataFilePath);
            
            if(!await _blockBlob.ExistsAsync())
            {
                throw new ApplicationException(string.Format("Could not find blob named {0}.", _configuration.ReferenceDataFilePath));
            }

            await _blockBlob.FetchAttributesAsync();

            if (_blockBlob.Properties.ETag == _blobETag)
            {
                return;
            }

            await LoadDictionaryAsync();
            _blobETag = _blockBlob.Properties.ETag;
        }

        private async Task LoadDictionaryAsync()
        {
            string text;
            using (var memoryStream = new MemoryStream())
            {
                await _blockBlob.DownloadToStreamAsync(memoryStream);
                text = Encoding.UTF8.GetString(memoryStream.ToArray());
            }

            var buildingMappings = JsonConvert.DeserializeObject<BuildingMapping[]>(text);

            foreach (var buildingMapping in buildingMappings)
            {
                _cachedBuildingDictionary[buildingMapping.DeviceId] = buildingMapping.BuildingId;
            }
        }
    }
}