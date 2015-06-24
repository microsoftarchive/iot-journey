// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Practices.IoTJourney.Tests.Common.Helpers
{
    public class AzureStorageHelpers
    {
        public static CloudTable GetAzureTable(string storageConnectionString, string tableName)
        {
            var storageClient = CloudStorageAccount.Parse(storageConnectionString);
            var client = storageClient.CreateCloudTableClient();
            return client.GetTableReference(tableName);
        }

        public static void DeleteAzureTable(CloudTable table)
        {
            table.DeleteIfExists();
        }

        public static async Task TouchBlobAsync(CloudBlockBlob blob)
        {
            await blob.FetchAttributesAsync();
            blob.Metadata.Add("touch", Guid.NewGuid().ToString("N"));
            await blob.SetMetadataAsync();
        }
    }
}