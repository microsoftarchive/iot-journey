// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Practices.IoTJourney.ColdStorage
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

        // Rolling blob writer settings
        public string BlobWriterStorageAccount { get; set; }
        public int RollSizeForBlobWriterMb { get; set; }
        public string ContainerName { get; set; }
        public string BlobPrefix { get; set; }

        // Circuit-breaker settings
        public int CircuitBreakerWarningLevel { get; set; }
        public int CircuitBreakerTripLevel { get; set; }
        public TimeSpan CircuitBreakerStallInterval { get; set; }
        public TimeSpan CircuitBreakerLogCooldownInterval { get; set; }

        public static Configuration GetCurrentConfiguration()
        {
            return new Configuration
            {
                EventHubConnectionString =
                    ConfigurationHelper.GetConfigValue<string>("Coldstorage.EventHubConnectionString"),
                EventHubName =
                    ConfigurationHelper.GetConfigValue<string>("Coldstorage.EventHubName"),
                CheckpointStorageAccount =
                    ConfigurationHelper.GetConfigValue<string>("Coldstorage.CheckpointStorageAccount"),
                ConsumerGroupName =
                    ConfigurationHelper.GetConfigValue<string>("Coldstorage.ConsumerGroupName", "ColdStorage"),
                MaxBatchSize =
                    ConfigurationHelper.GetConfigValue<int>("Coldstorage.MaxBatchSize"),
                PreFetchCount =
                    ConfigurationHelper.GetConfigValue<int>("Coldstorage.PreFetchCount"),
                ReceiveTimeout =
                    ConfigurationHelper.GetConfigValue("Coldstorage.ReceiveTimeout", TimeSpan.FromMinutes(7)),
                BlobWriterStorageAccount =
                    ConfigurationHelper.GetConfigValue<string>("Coldstorage.BlobWriterStorageAccount"),
                RollSizeForBlobWriterMb =
                    ConfigurationHelper.GetConfigValue<int>("Coldstorage.RollSizeForBlobWriterMb", _defaultRollSize),
                ContainerName =
                    ConfigurationHelper.GetConfigValue<string>("Coldstorage.ContainerName", "coldstorage"),
                BlobPrefix =
                    ConfigurationHelper.GetConfigValue<string>("Coldstorage.BlobPrefix", "pnp-iotjourney"),

                CircuitBreakerWarningLevel =
                    ConfigurationHelper.GetConfigValue<int>("Coldstorage.CircuitBreaker.WarningLevel", 200),
                CircuitBreakerTripLevel =
                    ConfigurationHelper.GetConfigValue<int>("Coldstorage.CircuitBreaker.TripLevel", 400),
                CircuitBreakerStallInterval =
                    ConfigurationHelper.GetConfigValue<TimeSpan>("Coldstorage.CircuitBreaker.StallInterval", TimeSpan.FromSeconds(30)),
                CircuitBreakerLogCooldownInterval =
                    ConfigurationHelper.GetConfigValue<TimeSpan>("Coldstorage.CircuitBreaker.LogCooldownInterval", TimeSpan.FromMinutes(15)),
            };
        }
    }
}