// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Configuration;
using System.Text;
using Microsoft.Practices.IoTJourney.ColdStorage.RollingBlobWriter;
using Microsoft.WindowsAzure.Storage;

namespace Microsoft.Practices.IoTJourney.ColdStorage.Tests
{
    public static class TestUtilities
    {
        public static CloudStorageAccount GetStorageAccount()
        {
            CloudStorageAccount storageAccount;
            var connectionString = ConfigurationManager.AppSettings["storageconnectionstring"];
            if (string.IsNullOrEmpty(connectionString)
                || connectionString.Contains("YourStorageAccountName")
                || !CloudStorageAccount.TryParse(connectionString, out storageAccount))
            {
                throw new ConfigurationErrorsException(
                    "Ensure the app setting with key 'storageconnectionstring' has a valid storage connection string as its value in the App.config file. It must be set in order to run integration tests.");
            }

            return storageAccount;
        }

        public static BlockData CreateBlockData(string payload, int blockSize)
        {
            var payloadFrame = new byte[blockSize];
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            payloadBytes.CopyTo(payloadFrame, 0L);
            return new BlockData(payloadFrame, payloadBytes.Length);
        }
    }
}