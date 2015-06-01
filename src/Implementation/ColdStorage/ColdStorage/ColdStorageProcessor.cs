// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.ColdStorage.Logging;
using Microsoft.Practices.IoTJourney.ColdStorage.RollingBlobWriter;
using Microsoft.Practices.IoTJourney.Devices.Events;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;

namespace Microsoft.Practices.IoTJourney.ColdStorage
{
    public class ColdStorageProcessor : IEventProcessor
    {
        public static readonly string EventDelimiter = Environment.NewLine;
        private const string ProcessorName = "writer";

        private const int MaxBlocks = 5;
        private const int MaxBlockSize = 4 * 1024 * 1024;

        private readonly Func<string, IBlobWriter> _blobWriterFactory;
        private readonly CancellationToken _token = CancellationToken.None;
        private readonly int _maxBlockSize;

        private readonly BufferManager _buffers;

        private IBlobWriter _blobWriter;
        private readonly List<BufferedFrameData> _eventHubBufferDataList = null;

        private byte[] _currentFrame;
        private int _currentFrameLength = 0;
        private EventData _lastEventData;

        // circuit breaker fields
        private readonly int _warningLevel;
        private readonly int _tripLevel;
        private readonly TimeSpan _stallInterval;
        private readonly TimeSpan _logCooldownInterval;
        private readonly string _eventHubName;
        private DateTime? _nextWarningLogTime;

        public ColdStorageProcessor(
            Func<string, IBlobWriter> blobWriterFactory,
            CancellationToken token,
            int warningLevel,
            int tripLevel,
            TimeSpan stallInterval,
            TimeSpan logCooldownInterval,
            string eventHubName,
            int maxBlocks = MaxBlocks,
            int maxBlockSize = MaxBlockSize)
        {
            Guard.ArgumentNotNull(blobWriterFactory, "blobWriterFactory");

            _blobWriterFactory = blobWriterFactory;
            _token = token;

            _warningLevel = warningLevel;
            _tripLevel = tripLevel;
            _stallInterval = stallInterval;
            _logCooldownInterval = logCooldownInterval;
            _eventHubName = eventHubName;

            _maxBlockSize = maxBlockSize;
            _buffers = BufferManager.CreateBufferManager(maxBlocks, _maxBlockSize);

            _eventHubBufferDataList = new List<BufferedFrameData>();
        }

        public Task OpenAsync(PartitionContext context)
        {
            ColdStorageEventSource.Log.LeaseObtained(ProcessorName, _eventHubName, context.Lease.PartitionId);

            // Generate a blob writer for this partition context
            _blobWriter = _blobWriterFactory(context.Lease.PartitionId);
            _currentFrame = _buffers.TakeBuffer(_maxBlockSize);
            _currentFrameLength = 0;
            _lastEventData = null;
            _nextWarningLogTime = null;

            return Task.FromResult(false);
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            // If we have partial blocks to flush, write them to our internal buffer
            if (_currentFrameLength > 0)
            {
                AddCurrentFrameToBuffer();
            }

            // If we are doing a clean shutdown (as opposed to losing the lease) flush 
            // our in-memory data to blob storage
            if (reason == CloseReason.Shutdown)
            {
                ColdStorageEventSource.Log.ShutDownInitiated(ProcessorName, _eventHubName, context.Lease.PartitionId);
                if (_eventHubBufferDataList.Count > 0)
                {
                    await FlushAndCheckPointAsync(context).ConfigureAwait(false);
                }
            }
            // If we just lost the lease, clear our data (the responsibility of writing this
            // partition's data has been assigned to another instance)
            else if (reason == CloseReason.LeaseLost)
            {
                ColdStorageEventSource.Log.LeaseLost(ProcessorName, _eventHubName, context.Lease.PartitionId);

                ClearBufferDataList();
            }

            foreach (var bufferedFrameData in _eventHubBufferDataList)
            {
                bufferedFrameData.Dispose();
            }
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> events)
        {
            // Workaround for event hub sending null on timeout
            events = events ?? Enumerable.Empty<EventData>();

            // Handle the timeout
            // we want this to happen before the circuit breaker
            if (!events.Any())
            {
                if (_currentFrameLength > 0)
                {
                    AddCurrentFrameToBuffer();

                    _currentFrame = _buffers.TakeBuffer(_maxBlockSize);
                    _currentFrameLength = 0;
                }

                ColdStorageEventSource.Log.ProcessorFlushingOnTimeout(_eventHubName, context.Lease.PartitionId, _eventHubBufferDataList.Count);

                // This means that we hit the receive timeout in which case we need to 
                // write ( EventProcessorOptions.InvokeProcessorAfterReceiveTimeout should be set)
                await FlushAndCheckPointAsync(context).ConfigureAwait(false);

                return;
            }

            // Check the circuit breaker to see if we need to stall processing (in case of
            // sustained storage issues)
            await CheckBreak(context).ConfigureAwait(false);

            int processedEvents = 0;

            // Handle incoming events
            foreach (var e in events)
            {
                processedEvents++;

                byte[] bytes = null;
                try
                {
                    bytes = GetBytesInEvent(e);
                }
                catch (Exception ex)
                {
                    var msgId = String.Concat(_eventHubName, "/", e.PartitionKey, "/", e.Offset);
                    ColdStorageEventSource.Log.CouldNotDeserialize(ex, msgId);
                    continue;
                }

                // If we have exceeded the max block size for this buffer, flush
                if ((bytes.Length + _currentFrameLength) >= _maxBlockSize)
                {
                    // Flush what you have so far
                    AddCurrentFrameToBuffer();

                    await FlushAndCheckPointAsync(context).ConfigureAwait(false);
                    _currentFrame = _buffers.TakeBuffer(_maxBlockSize);
                    _currentFrameLength = 0;
                }

                Buffer.BlockCopy(bytes, 0, _currentFrame, _currentFrameLength, bytes.Length);
                _currentFrameLength += bytes.Length;
                _lastEventData = e;
            }

        }

        // You should provide your own implementation of this method if you need 
        // to customize the serialization.
        private static byte[] GetBytesInEvent(EventData eData)
        {
            var eventToPersist = new ColdStorageEvent();
            eventToPersist.Offset = eData.Offset;
            eventToPersist.Properties = GetStringDictionary(eData.Properties);
            try
            {
                eventToPersist.Payload = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(eData.GetBytes()));
            }
            catch (JsonReaderException)
            {
                eventToPersist.Payload = Encoding.UTF8.GetString(eData.GetBytes());
            }
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventToPersist) + EventDelimiter);
        }

        private static Dictionary<string, string> GetStringDictionary(IDictionary<string, object> dictionary)
        {
            var stringDictionary = new Dictionary<string, string>();
            if (dictionary != null)
            {
                foreach (var entry in dictionary)
                {
                    stringDictionary.Add(entry.Key, entry.Value.ToString());
                }
            }
            return stringDictionary;
        }

        private async Task FlushAndCheckPointAsync(PartitionContext context)
        {
            // If we have no data to flush, return immediately
            if (_eventHubBufferDataList.Count == 0)
            {
                return;
            }

            if (!await _blobWriter.WriteAsync(_eventHubBufferDataList, _token).ConfigureAwait(false))
            {
                //TODO Do retries with a circut breaker and recycle role if 
                // you cannot write after x frames are buffered and cleear any interim state to 
                // keep buffering

                return;
            }

            EventData checkpointEventData = ClearBufferDataList();

            try
            {
                await context.CheckpointAsync(checkpointEventData);
                ColdStorageEventSource.Log.CheckpointCompleted(ProcessorName, _eventHubName, context.Lease.PartitionId, checkpointEventData.Offset);
            }
            catch (Exception ex)
            {
                if (!(ex is StorageException || ex is LeaseLostException))
                {
                    throw;
                }

                ColdStorageEventSource.Log.UnableToCheckpoint(ex, ProcessorName, _eventHubName, context.Lease.PartitionId);
            }
        }

        private void AddCurrentFrameToBuffer()
        {
            var newData = new BufferedFrameData(_currentFrame, _currentFrameLength, _lastEventData);
            _eventHubBufferDataList.Add(newData);
        }

        private EventData ClearBufferDataList()
        {
            var frame = _eventHubBufferDataList.LastOrDefault();
            _eventHubBufferDataList.ForEach(b => _buffers.ReturnBuffer(b.Frame));

            _eventHubBufferDataList.Clear();

            return frame != null ? frame.LastEventDataInFrame : null;
        }

        private async Task CheckBreak(PartitionContext partitionContext)
        {
            string partitionId = partitionContext.Lease.PartitionId;

            var currentLevel = _eventHubBufferDataList.Count;
            if (currentLevel < _warningLevel)
            {
                // Circuit is closed

                ColdStorageEventSource.Log.CircuitBreakerClosed(ProcessorName, partitionId, currentLevel);
                _nextWarningLogTime = null;

                return;
            }
            else if (currentLevel < _tripLevel)
            {
                // Circuit is closed, but log a warning if appropriate

                if (_nextWarningLogTime == null || _nextWarningLogTime.Value <= DateTime.UtcNow)
                {
                    ColdStorageEventSource.Log.CircuitBreakerWarning(ProcessorName, partitionId, _warningLevel, currentLevel);
                    _nextWarningLogTime = DateTime.UtcNow.Add(_logCooldownInterval);
                }

                return;
            }

            // Circuit is open. Stall until conditions are restored, logging an update when appropriate

            ColdStorageEventSource.Log.CircuitBreakerTripped(ProcessorName, partitionId, _tripLevel, currentLevel);
            var nextErrorLogTime = DateTime.UtcNow.Add(_logCooldownInterval);

            while (true)
            {
                await Task.Delay(_stallInterval);

                await FlushAndCheckPointAsync(partitionContext);

                currentLevel = _eventHubBufferDataList.Count;

                if (currentLevel < _warningLevel)
                {
                    ColdStorageEventSource.Log.CircuitBreakerRestored(ProcessorName, partitionId, _warningLevel, currentLevel);
                    _nextWarningLogTime = null;
                    return;
                }

                var now = DateTime.UtcNow;
                if (nextErrorLogTime <= now)
                {
                    ColdStorageEventSource.Log.CircuitBreakerTripped(ProcessorName, partitionId, _tripLevel, currentLevel);
                    nextErrorLogTime = now.Add(_logCooldownInterval);
                }
            }
        }
    }
}