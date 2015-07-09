// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Practices.IoTJourney.WarmStorage
{
    public class Configuration
    {
        private const int _defaultRollSize = 2048;

        // Eventhub settings
        public string ConsumerGroupName { get; set; }
        public string CheckpointStorageAccount { get; set; }
        public string EventHubConnectionString { get; set; }
        public string EventHubName { get; set; }
        public int MaxBatchSize { get; set; }
        public int PreFetchCount { get; set; }
        public TimeSpan ReceiveTimeout { get; set; }
        public string ElasticSearchUrl { get; set; }
        public string ElasticSearchIndexPrefix { get; set; }
        public string ElasticSearchIndexType { get; set; }
        public int RetryCount { get; set; }
        public string ReferenceDataStorageAccount { get; set; }
        public string ReferenceDataStorageContainer { get; set; }
        public string ReferenceDataFilePath { get; set; }
        public int ReferenceDataCacheTTLMinutes { get; set; }

        public static Configuration GetCurrentConfiguration()
        {
            return new Configuration
            {
                EventHubConnectionString = ConfigurationHelper.GetConfigValue<string>("Warmstorage.EventHubConnectionString"),
                EventHubName = ConfigurationHelper.GetConfigValue<string>("Warmstorage.EventHubName"),
                CheckpointStorageAccount = ConfigurationHelper.GetConfigValue<string>("Warmstorage.CheckpointStorageAccount"),
                ConsumerGroupName = ConfigurationHelper.GetConfigValue<string>("Warmstorage.ConsumerGroupName", "WarmStorage"),
                MaxBatchSize = ConfigurationHelper.GetConfigValue<int>("Warmstorage.MaxBatchSize"),
                PreFetchCount = ConfigurationHelper.GetConfigValue<int>("Warmstorage.PreFetchCount"),
                ReceiveTimeout = ConfigurationHelper.GetConfigValue("Warmstorage.ReceiveTimeout", TimeSpan.FromMinutes(7)),
                ElasticSearchUrl = ConfigurationHelper.GetConfigValue<string>("Warmstorage.ElasticSearchUri", "http://localhost:9200"),
                ElasticSearchIndexPrefix = ConfigurationHelper.GetConfigValue<string>("Warmstorage.ElasticSearchIndexPrefix", "iot"),
                ElasticSearchIndexType = ConfigurationHelper.GetConfigValue<string>("Warmstorage.ElasticSearchIndexType", "temperature"),
                RetryCount = ConfigurationHelper.GetConfigValue<int>("Warmstorage.RetryCount", 3),
                ReferenceDataStorageAccount = ConfigurationHelper.GetConfigValue<string>("Warmstorage.CheckpointStorageAccount"),
                ReferenceDataStorageContainer = ConfigurationHelper.GetConfigValue<string>("Warmstorage.ReferenceDataStorageContainer"),
                ReferenceDataFilePath = ConfigurationHelper.GetConfigValue<string>("Warmstorage.ReferenceDataFilePath"),
                ReferenceDataCacheTTLMinutes = ConfigurationHelper.GetConfigValue<int>("WarmStorage.ReferenceDataCacheTTLMinutes")
            };
        }
    }
}
