// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Practices.IoTJourney.ColdStorage.EventProcessor.Logging
{
    public class ColdStorageEventSource : EventSource
    {
        public class Keywords
        {
            public const EventKeywords Performance = (EventKeywords)1;
        }

        public static ColdStorageEventSource Log = new ColdStorageEventSource();

        [Event(1, Level = EventLevel.Informational, Message = "Processor for partition {1} in hub {0} invoked without events. Flushing {2} cached blocks.")]
        public void ProcessorFlushingOnTimeout(string eventHubName, string partitionId, int blockCount)
        {
            WriteEvent(1, eventHubName, partitionId, blockCount);
        }

        [Event(2, Level = EventLevel.Informational, Message = "Processor {0} for partition {2} on hub {1} completed a checkpoint for offset {3}.")]
        public void CheckpointCompleted(string processorName, string eventHubName, string partitionId, string offset)
        {
            WriteEvent(2, processorName, eventHubName, partitionId, offset);
        }

        [NonEvent]
        public void UnableToCheckpoint(Exception exception, string processorName, string eventHubName, string partitionId)
        {
            UnableToCheckpoint_Inner(exception.ToString(), processorName, eventHubName, partitionId);
        }

        [Event(3, Level = EventLevel.Warning, Message = "Processor {0} for partition {2} on hub {1} could not checkpoint.")]
        public void UnableToCheckpoint_Inner(string exceptionString, string processorName, string eventHubName, string partitionId)
        {
            WriteEvent(3, exceptionString, processorName, eventHubName, partitionId);
        }

        [Event(4, Level = EventLevel.Informational, Message = "Processor {0} obtained a lease on partition {2} on hub {1}.")]
        public void LeaseObtained(string processorName, string eventHubName, string partitionId)
        {
            WriteEvent(4, processorName, eventHubName, partitionId);
        }

        [Event(5, Level = EventLevel.Warning, Message = "Processor {0} lost a lease on partition {2} on hub {1}.")]
        public void LeaseLost(string processorName, string eventHubName, string partitionId)
        {
            WriteEvent(5, processorName, eventHubName, partitionId);
        }

        [Event(6, Level = EventLevel.Informational, Message = "Processor {0} shutting down on partition {2} on hub {1}.")]
        public void ShutDownInitiated(string processorName, string eventHubName, string partitionId)
        {
            WriteEvent(6, processorName, eventHubName, partitionId);
        }

        [Event(7, Level = EventLevel.Verbose, Message = "Started writing to blob {0} having {1} existing blocks writing {2} new blocks with total {3} bytes")]
        public void WriteToBlobStarted(string blobName, int blockListLength, int numberOfBlocks, long bytesLength)
        {
            WriteEvent(7, blobName, blockListLength, numberOfBlocks, bytesLength);
        }

        [Event(8, Level = EventLevel.Verbose, Message = "Completed Writing to blob {0} total block length is now {1}")]
        public void WriteToBlobEnded(string blobName, int blockListLength)
        {
            WriteEvent(8, blobName, blockListLength);
        }

        [NonEvent]
        public void WriteToBlobFailed(Exception exception, string blobName, int numberOfBlocks, long bytesLength)
        {
            WriteToBlobFailed_Inner(exception.ToString(), blobName, numberOfBlocks, bytesLength);
        }

        [Event(9, Level = EventLevel.Error, Message = "Failed writing to blob {0} blocks {1} bytes {2}")]
        public void WriteToBlobFailed_Inner(string exceptionString, string blobName, int numberOfBlocks, long bytesLength)
        {
            WriteEvent(9, exceptionString, blobName, numberOfBlocks, bytesLength);
        }

        [Event(10, Level = EventLevel.Informational, Message = "Rolling over from blob {0} to blob {1}")]
        public void RollOccured(string oldBlobName, string newBlobName)
        {
            WriteEvent(10, oldBlobName, newBlobName);
        }

        [Event(11, Level = EventLevel.Error, Message = "Etag mismatch writing to blob {0}")]
        public void BlobEtagMissMatchOccured(string blobName)
        {
            WriteEvent(11, blobName);
        }

        [NonEvent]
        public void HardStorageExceptionCaughtWritingToBlob(StorageException storageException, CloudBlobClient blobClient, string containerName)
        {
            HardStorageExceptionCaughtWritingToBlob_Inner(storageException.ToString(), GetStorageAccountName(blobClient), containerName, storageException.RequestInformation.ExtendedErrorInformation.ErrorCode);
        }

        [Event(12, Level = EventLevel.Error, Message = "Error attempting storage operation on container {1} for storage account {0}: {2}")]
        public void HardStorageExceptionCaughtWritingToBlob_Inner(string storageExceptionString, string storageAccountName, string containerName, string errorCode)
        {
            WriteEvent(12, storageExceptionString, storageAccountName, containerName, errorCode);
        }

        [NonEvent]
        public void StorageExceptionCaughtWritingToBlob(StorageException storageException, CloudBlobClient blobClient, string containerName)
        {
            StorageExceptionCaughtWritingToBlob_Inner(storageException.ToString(), GetStorageAccountName(blobClient), containerName, storageException.RequestInformation.ExtendedErrorInformation.ErrorCode);
        }

        [Event(13, Level = EventLevel.Warning, Message = "Error attempting storage operation on container {1} for storage account {0}: {2}. Request is ignored.")]
        public void StorageExceptionCaughtWritingToBlob_Inner(string storageExceptionString, string storageAccountName, string containerName, string errorCode)
        {
            WriteEvent(13, storageExceptionString, storageAccountName, containerName, errorCode);
        }

        private static string GetStorageAccountName(CloudBlobClient blobClient)
        {
            return blobClient.StorageUri.PrimaryUri.Authority.Split('.')[0];
        }

        [NonEvent]
        public void CircuitBreakerInitialized(string processorName, string partitionId, int warningLevel, int tripLevel, TimeSpan stallInterval, TimeSpan logCooldownInterval)
        {
            CircuitBreakerInitialized_Inner(processorName, partitionId, warningLevel, tripLevel, stallInterval.Ticks, logCooldownInterval.Ticks);
        }

        [Event(14, Level = EventLevel.Informational, Message = "Processor {0} for partition {1} circuit breaker initialized. Warning level {2}, break level {3}, stall interval {4}, log cooldown interval {5}.")]
        public void CircuitBreakerInitialized_Inner(string processorName, string partitionId, int warningLevel, int tripLevel, long stallIntervalTicks, long logCooldownIntervalTicks)
        {
            WriteEvent(14, processorName, partitionId, warningLevel, tripLevel, stallIntervalTicks, logCooldownIntervalTicks);
        }

        [Event(15, Level = EventLevel.Verbose, Message = "Processor {0} for partition {1} circuit closed. Current level {2}.")]
        public void CircuitBreakerClosed(string processorName, string partitionId, int currentLevel)
        {
            WriteEvent(15, processorName, partitionId, currentLevel);
        }

        [Event(16, Level = EventLevel.Warning, Message = "Processor {0} for partition {1} over warning level {2}. Current level {3}.")]
        public void CircuitBreakerWarning(string processorName, string partitionId, int warningLevel, int currentLevel)
        {
            WriteEvent(16, processorName, partitionId, warningLevel, currentLevel);
        }

        [Event(17, Level = EventLevel.Error, Message = "Processor {0} for partition {1} over break level {2}. Circuit broken, current level {3}.")]
        public void CircuitBreakerTripped(string processorName, string partitionId, int tripLevel, int currentLevel)
        {
            WriteEvent(17, processorName, partitionId, tripLevel, currentLevel);
        }

        [NonEvent]
        public void CircuitBreakerStalling(string processorName, string partitionId, int tripLevel, int currentLevel, TimeSpan stallInterval)
        {
            CircuitBreakerStalling_Inner(processorName, partitionId, tripLevel, currentLevel, stallInterval.Ticks);
        }

        [Event(18, Level = EventLevel.Verbose, Message = "Processor {0} for partition {1} stalling for {2}. Current level {3}.")]
        public void CircuitBreakerStalling_Inner(string processorName, string partitionId, int tripLevel, int currentLevel, long stallIntervalTicks)
        {
            WriteEvent(18, processorName, partitionId, tripLevel, currentLevel, stallIntervalTicks);
        }

        [Event(19, Level = EventLevel.Informational, Message = "Processor {0} for partition {1} under warning level {2}. Processing restored, current level {3}.")]
        public void CircuitBreakerRestored(string processorName, string partitionId, int warningLevel, int currentLevel)
        {
            WriteEvent(19, processorName, partitionId, warningLevel, currentLevel);
        }

        [NonEvent]
        public void CouldNotDeserialize(Exception ex, string msgId)
        {
            CouldNotDeserialize_Inner(ex.ToString(), msgId);
        }

        [Event(20, Level = EventLevel.Warning, Message = "Could not deserialize {0}")]
        public void CouldNotDeserialize_Inner(string exceptionString, string msgId)
        {
            WriteEvent(20, exceptionString, msgId);
        }

        [Event(21, Level = EventLevel.Informational, Message = "Initializing event hub listener for {0} ({1})")]
        public void InitializingEventHubListener(string eventHubName, string consumerGroupName)
        {
            WriteEvent(21, eventHubName, consumerGroupName);
        }

        [NonEvent]
        public void InvalidEventHubConsumerGroupName(Exception exception, string eventHubName, string consumerGroupName)
        {
            InvalidEventHubConsumerGroupName_Inner(exception.ToString(), eventHubName, consumerGroupName);
        }

        [Event(22, Level = EventLevel.Error, Message = "Invalid consumer group name {0} in event hub {1}")]
        public void InvalidEventHubConsumerGroupName_Inner(string exceptionString, string eventHubName, string consumerGroupName)
        {
            WriteEvent(22, exceptionString, eventHubName, consumerGroupName);
        }

        [NonEvent]
        public void ErrorProcessingMessage(Exception exception, string actionName)
        {
            ErrorProcessingMessage_Inner(exception.ToString(), actionName);
        }

        [Event(23, Level = EventLevel.Error, Message = "Error on message processing, action {0}")]
        public void ErrorProcessingMessage_Inner(string exceptionString, string actionName)
        {
            WriteEvent(23, exceptionString, actionName);
        }

        [Event(24, Level = EventLevel.Informational, Message = "Found consumer group {1} for {0}")]
        public void ConsumerGroupFound(string eventHubName, string consumerGroupName)
        {
            WriteEvent(24, eventHubName, consumerGroupName);
        }

        [Event(25, Level = EventLevel.Verbose, Keywords = Keywords.Performance, Message = "{0}")]
        public void WriteToBlobEndedPerf(long bytes)
        {
            WriteEvent(25, bytes);
        }
    }
}