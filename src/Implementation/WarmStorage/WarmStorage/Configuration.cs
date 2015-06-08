using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                RetryCount = ConfigurationHelper.GetConfigValue<int>("Warmstorage.RetryCount", 3)
            };
        }
    }
}
