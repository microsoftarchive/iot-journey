using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.WarmStorage.EventProcessor.Logging
{
    public class WarmStorageEventSource : EventSource
    {
        public class Keywords
        {
            public const EventKeywords Performance = (EventKeywords)1;
        }

        public static WarmStorageEventSource Log = new WarmStorageEventSource();

        [Event(1, Level = EventLevel.Informational, Message = "Processor {0} for partition {2} on hub {1} completed a checkpoint for offset {3}.")]
        public void CheckpointCompleted(string processorName, string eventHubName, string partitionId, string offset)
        {
            WriteEvent(1, processorName, eventHubName, partitionId, offset);
        }

        [NonEvent]
        public void UnableToCheckpoint(Exception exception, string processorName, string eventHubName, string partitionId)
        {
            UnableToCheckpoint_Inner(exception.ToString(), processorName, eventHubName, partitionId);
        }

        [Event(2, Level = EventLevel.Warning, Message = "Processor {0} for partition {2} on hub {1} could not checkpoint.")]
        public void UnableToCheckpoint_Inner(string exceptionString, string processorName, string eventHubName, string partitionId)
        {
            WriteEvent(2, exceptionString, processorName, eventHubName, partitionId);
        }

        [Event(3, Level = EventLevel.Informational, Message = "Processor {0} obtained a lease on partition {2} on hub {1}.")]
        public void LeaseObtained(string processorName, string eventHubName, string partitionId)
        {
            WriteEvent(3, processorName, eventHubName, partitionId);
        }

        [Event(4, Level = EventLevel.Warning, Message = "Processor {0} lost a lease on partition {2} on hub {1}.")]
        public void LeaseLost(string processorName, string eventHubName, string partitionId)
        {
            WriteEvent(4, processorName, eventHubName, partitionId);
        }

        [Event(5, Level = EventLevel.Informational, Message = "Processor {0} shutting down on partition {2} on hub {1}.")]
        public void ShutDownInitiated(string processorName, string eventHubName, string partitionId)
        {
            WriteEvent(5, processorName, eventHubName, partitionId);
        }

        [NonEvent]
        public void CouldNotDeserialize(Exception ex, string msgId)
        {
            CouldNotDeserialize_Inner(ex.ToString(), msgId);
        }

        [Event(6, Level = EventLevel.Warning, Message = "Could not deserialize {0}")]
        public void CouldNotDeserialize_Inner(string exceptionString, string msgId)
        {
            WriteEvent(6, exceptionString, msgId);
        }

        [Event(7, Level = EventLevel.Informational, Message = "Initializing event hub listener for {0} ({1})")]
        public void InitializingEventHubListener(string eventHubName, string consumerGroupName)
        {
            WriteEvent(7, eventHubName, consumerGroupName);
        }

        [NonEvent]
        public void InvalidEventHubConsumerGroupName(Exception exception, string eventHubName, string consumerGroupName)
        {
            InvalidEventHubConsumerGroupName_Inner(exception.ToString(), eventHubName, consumerGroupName);
        }

        [Event(8, Level = EventLevel.Error, Message = "Invalid consumer group name {0} in event hub {1}")]
        public void InvalidEventHubConsumerGroupName_Inner(string exceptionString, string eventHubName, string consumerGroupName)
        {
            WriteEvent(8, exceptionString, eventHubName, consumerGroupName);
        }

        [NonEvent]
        public void ErrorProcessingMessage(Exception exception, string actionName)
        {
            ErrorProcessingMessage_Inner(exception.ToString(), actionName);
        }

        [Event(9, Level = EventLevel.Error, Message = "Error on message processing, action {0}")]
        public void ErrorProcessingMessage_Inner(string exceptionString, string actionName)
        {
            WriteEvent(9, exceptionString, actionName);
        }

        [Event(10, Level = EventLevel.Informational, Message = "Found consumer group {1} for {0}")]
        public void ConsumerGroupFound(string eventHubName, string consumerGroupName)
        {
            WriteEvent(10, eventHubName, consumerGroupName);
        }

        [Event(11, Level = EventLevel.Error, Message = "Error writing to Elastic Search. Server response failure.")]
        public void WriteToElasticSearchResponseFailed(string serverErrorMessage)
        {
            WriteEvent(11, serverErrorMessage);
        }

        [NonEvent]
        public void WriteToElasticSearchError(Exception exception)
        {
            WriteToElasticSearchError_Inner(exception.ToString());
        }

        [Event(12, Level = EventLevel.Error, Message = "Error writing to Elastic Search. An exception has ocurred.")]
        public void WriteToElasticSearchError_Inner(string exceptionString)
        {
            WriteEvent(12, exceptionString);
        }

        [Event(9100, Level = EventLevel.Verbose, Keywords = Keywords.Performance, Message = "{0}")]
        public void WriteToElasticSearchSuccessPerf(int messageCount)
        {
            WriteEvent(9100, messageCount);
        }

        [Event(9101, Level = EventLevel.Verbose, Keywords = Keywords.Performance, Message = "{0}")]
        public void WriteToElasticSearchFailedPerf(int messageCount)
        {
            WriteEvent(9101, messageCount);
        }
    }
}
