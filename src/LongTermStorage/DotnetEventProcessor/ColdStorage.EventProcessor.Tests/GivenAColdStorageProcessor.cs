// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.ColdStorage.EventProcessor.RollingBlobWriter;
using Microsoft.Practices.IoTJourney.Devices.Events;
using Microsoft.Practices.IoTJourney.Tests.Common;
using Microsoft.Practices.IoTJourney.Tests.Common.Helpers;
using Microsoft.Practices.IoTJourney.Tests.Common.Mocks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Practices.IoTJourney.ColdStorage.EventProcessor.Tests
{
    public partial class GivenAColdStorageProcessor
    {
        private const int MaxBlockSize = 1000;

        private ColdStorageProcessor _processor;
        private Mock<IBlobWriter> _writerMock;
        private const string checkpoint = "checkpoint";
        private const string write = "write";

        public GivenAColdStorageProcessor()
        {
            _writerMock = new Mock<IBlobWriter>();
            _processor =
                new ColdStorageProcessor(
                    n => _writerMock.Object,
                    CircuitBreakerWarningLevel,
                    CircuitBreakerStallLevel,
                    CircuitBreakerStallInterval,
                    TimeSpan.FromSeconds(200),
                    "Test",
                    maxBlockSize: MaxBlockSize);
        }

        [Fact]
        [Trait("Running time", "Short")]
        [Trait("Category", "Unit")]
        public async Task WhenIncomingMessagesFailToFillABlock_ThenDoesNotWrite()
        {
            var context = MockPartitionContext.CreateWithNoopCheckpoint("0");
            await _processor.OpenAsync(context);

            await _processor.ProcessEventsAsync(
                context,
                new[] 
                {
                    CreateEventData((byte)'a', 100),
                    CreateEventData((byte)'b', 200),
                    CreateEventData((byte)'c', 300),
                });

            _writerMock.Verify(
                w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()),
                Times.Never());
        }

        [Fact]
        [Trait("Running time", "Short")]
        [Trait("Category", "Unit")]
        public async Task WhenIncomingMessagesFillABlock_ThenWritesFilledBlock()
        {
            // Arrange
            var operationQueue = new List<string>();
            IList<BlockData> blocks = null;
            Action<IReadOnlyCollection<BlockData>, CancellationToken> flagWriteOperation = (d, ct) =>
                {
                    operationQueue.Add(write);
                    blocks = d.ToList();
                };

            Func<Task> flagCheckpointOperation = () =>
                {
                    operationQueue.Add(checkpoint);
                    return Task.FromResult(true);
                };

            var context = MockPartitionContext.Create("0", flagCheckpointOperation);
            await _processor.OpenAsync(context);

            _writerMock
                .Setup(w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Callback(flagWriteOperation);

            // Act
            await _processor.ProcessEventsAsync(
                context,
                new[] 
                {
                    CreateEventData((byte)'a', 100),
                    CreateEventData((byte)'b', 200),
                    CreateEventData((byte)'c', 300),
                    CreateEventData((byte)'d', 400),
                });

            Assert.NotNull(blocks);
            Assert.Equal(1, blocks.Count);

            var serializedFrame = Encoding.UTF8.GetString(blocks[0].Frame, 0, blocks[0].FrameLength);
            var lines = serializedFrame.Split(new[] { ColdStorageProcessor.EventDelimiter }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(3, lines.Length);

            Assert.Equal(new string('a', 100), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[0])).Payload);
            Assert.Equal(new string('b', 200), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[1])).Payload);
            Assert.Equal(new string('c', 300), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[2])).Payload);

            // Assert checkpoint happens after write
            Assert.Equal(write, operationQueue.FirstOrDefault());
            Assert.Equal(checkpoint, operationQueue.LastOrDefault());
        }

        [Fact]
        [Trait("Running time", "Short")]
        [Trait("Category", "Unit")]
        public async Task WhenClosingForShutdown_ThenFlushesRemainingBuffer()
        {
            var operationQueue = new List<string>();
            IList<BlockData> blocks = null;
            Action<IReadOnlyCollection<BlockData>, CancellationToken> flagWriteOperation = (d, ct) =>
            {
                operationQueue.Add(write);
                blocks = d.ToList();
            };

            Func<Task> flagCheckpointOperation = () =>
            {
                operationQueue.Add(checkpoint);
                return Task.FromResult(true);
            };

            var context = MockPartitionContext.Create("0", flagCheckpointOperation);
            await _processor.OpenAsync(context);

            _writerMock
                .Setup(w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Callback(flagWriteOperation);

            await _processor.ProcessEventsAsync(
                context,
                new[] 
                {
                    CreateEventData((byte)'a', 100),
                    CreateEventData((byte)'b', 200),
                    CreateEventData((byte)'c', 300),
                });

            _writerMock.Verify(
                w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()),
                Times.Never());

            await _processor.CloseAsync(context, CloseReason.Shutdown);

            var serializedFrame = Encoding.UTF8.GetString(blocks[0].Frame, 0, blocks[0].FrameLength);
            var lines = serializedFrame.Split(new[] { ColdStorageProcessor.EventDelimiter }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(3, lines.Length);

            Assert.Equal(new string('a', 100), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[0])).Payload);
            Assert.Equal(new string('b', 200), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[1])).Payload);
            Assert.Equal(new string('c', 300), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[2])).Payload);

            // Assert checkpoint happens after write
            Assert.Equal(write, operationQueue.FirstOrDefault());
            Assert.Equal(checkpoint, operationQueue.LastOrDefault());
        }

        [Fact]
        [Trait("Running time", "Short")]
        [Trait("Category", "Unit")]
        public async Task WhenReceivesNullEvents_ThenFlushesRemainingBuffer()
        {
            var operationQueue = new List<string>();
            IList<BlockData> blocks = null;
            Action<IReadOnlyCollection<BlockData>, CancellationToken> flagWriteOperation = (d, ct) =>
            {
                operationQueue.Add(write);
                blocks = d.ToList();
            };

            Func<Task> flagCheckpointOperation = () =>
            {
                operationQueue.Add(checkpoint);
                return Task.FromResult(true);
            };

            var context = MockPartitionContext.Create("0", flagCheckpointOperation);
            await _processor.OpenAsync(context);

            _writerMock
                .Setup(w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Callback(flagWriteOperation);

            await _processor.ProcessEventsAsync(
                context,
                new[] 
                {
                    CreateEventData((byte)'a', 100),
                    CreateEventData((byte)'b', 200),
                    CreateEventData((byte)'c', 300),
                });

            _writerMock.Verify(
                w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()),
                Times.Never());

            await _processor.ProcessEventsAsync(context, null);

            var serializedFrame = Encoding.UTF8.GetString(blocks[0].Frame, 0, blocks[0].FrameLength);
            var lines = serializedFrame.Split(new[] { ColdStorageProcessor.EventDelimiter }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(3, lines.Length);

            Assert.Equal(new string('a', 100), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[0])).Payload);
            Assert.Equal(new string('b', 200), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[1])).Payload);
            Assert.Equal(new string('c', 300), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[2])).Payload);

            // Assert checkpoint happens after write
            Assert.Equal(write, operationQueue.FirstOrDefault());
            Assert.Equal(checkpoint, operationQueue.LastOrDefault());
        }

        [Fact]
        [Trait("Running time", "Short")]
        [Trait("Category", "Unit")]
        public async Task WhenReceivesNullEvents_ThenDoesNotFlushEmptyBuffer()
        {
            var operationQueue = new List<string>();
            IList<BlockData> blocks = null;
            Action<IReadOnlyCollection<BlockData>, CancellationToken> flagWriteOperation = (d, ct) =>
            {
                operationQueue.Add(write);
                blocks = d.ToList();
            };

            Func<Task> flagCheckpointOperation = () =>
            {
                operationQueue.Add(checkpoint);
                return Task.FromResult(true);
            };

            var context = MockPartitionContext.Create("0", flagCheckpointOperation);
            await _processor.OpenAsync(context);

            _writerMock
                .Setup(w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Callback(flagWriteOperation);

            await _processor.ProcessEventsAsync(context, null);

            _writerMock.Verify(
                w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()),
                Times.Never());

            // Assert checkpoint is never called
            Assert.Empty(operationQueue);
        }

        [Fact]
        [Trait("Running time", "Short")]
        [Trait("Category", "Unit")]
        public async Task WhenClosingForLostLease_ThenDoesNotFlushRemainingBuffer()
        {
            var operationQueue = new List<string>();

            Func<Task> flagCheckpointOperation = () =>
                {
                    operationQueue.Add(checkpoint);
                    return Task.FromResult(true);
                };

            IList<BlockData> blocks = null;
            Action<IReadOnlyCollection<BlockData>, CancellationToken> getBlocks = (d, ct) => blocks = d.ToList();

            var context = MockPartitionContext.Create("0", flagCheckpointOperation);
            await _processor.OpenAsync(context);

            _writerMock
                .Setup(w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Callback(getBlocks);

            await _processor.ProcessEventsAsync(
                context,
                new[] 
                {
                    CreateEventData((byte)'a', 100),
                    CreateEventData((byte)'b', 200),
                    CreateEventData((byte)'c', 300),
                });

            _writerMock.Verify(
                w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()),
                Times.Never());

            await _processor.CloseAsync(context, CloseReason.LeaseLost);

            _writerMock.Verify(
                w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()),
                Times.Never());

            // Assert checkpoint is never called
            Assert.Empty(operationQueue);
        }

        [Fact]
        [Trait("Running time", "Short")]
        [Trait("Category", "Unit")]
        public async Task WhenClosingForLostLeaseWithEmptyBuffer_ThenDoesThrow()
        {
            var operationQueue = new List<string>();

            Func<Task> flagCheckpointOperation = () =>
            {
                operationQueue.Add(checkpoint);
                return Task.FromResult(true);
            };

            IList<BlockData> blocks = null;
            Action<IReadOnlyCollection<BlockData>, CancellationToken> getBlocks = (d, ct) => blocks = d.ToList();

            var context = MockPartitionContext.Create("0", flagCheckpointOperation);
            await _processor.OpenAsync(context);

            _writerMock
                .Setup(w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Callback(getBlocks);

            await _processor.CloseAsync(context, CloseReason.LeaseLost);

            _writerMock.Verify(
                w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()),
                Times.Never());

            // Assert checkpoint is never called
            Assert.Empty(operationQueue);
        }

        [Fact]
        [Trait("Running time", "Short")]
        [Trait("Category", "Unit")]
        public async Task WhenWritingFullBlockFails_ThenDoesNotThrow()
        {
            // Arrange
            var operationQueue = new List<string>();

            Func<Task> flagCheckpointOperation = () =>
            {
                operationQueue.Add(checkpoint);
                return Task.FromResult(true);
            };

            IList<BlockData> blocks = null;
            Action<IReadOnlyCollection<BlockData>, CancellationToken> getBlocks = (d, ct) => blocks = d.ToList();

            var context = MockPartitionContext.Create("0", flagCheckpointOperation);
            await _processor.OpenAsync(context);

            _writerMock
                .Setup(w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()))
                .Returns(() => TaskHelpers.CreateCompletedTask(false))
                .Callback(getBlocks);

            // Act
            await _processor.ProcessEventsAsync(
                context,
                new[] 
                {
                    CreateEventData((byte)'a', 100),
                    CreateEventData((byte)'b', 200),
                    CreateEventData((byte)'c', 300),
                    CreateEventData((byte)'d', 400),
                });

            // Assert
            Assert.NotNull(blocks);
            Assert.Equal(1, blocks.Count);
            Assert.False(operationQueue.Contains(checkpoint));
        }

        [Fact]
        [Trait("Running time", "Short")]
        [Trait("Category", "Unit")]
        public async Task WhenWritingFullBlockFails_ThenReattemptsOnNextFilledBlock()
        {
            // Arrange
            var operationQueue = new List<string>();
            Func<Task> flagCheckpointOperation = () =>
                {
                    operationQueue.Add(checkpoint);
                    return Task.FromResult(true);
                };

            IList<BlockData> blocks = null;
            Action<IReadOnlyCollection<BlockData>, CancellationToken> getBlocks = (d, ct) => blocks = d.ToList();

            var context = MockPartitionContext.Create("0", flagCheckpointOperation);
            await _processor.OpenAsync(context);

            _writerMock
                .Setup(w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()))
                .Returns(() => TaskHelpers.CreateCompletedTask(false))
                .Callback(getBlocks);

            // Act
            await _processor.ProcessEventsAsync(
                context,
                new[] 
                {
                    CreateEventData((byte)'a', 100),
                    CreateEventData((byte)'b', 200),
                    CreateEventData((byte)'c', 300),
                    CreateEventData((byte)'d', 400),
                });

            Assert.NotNull(blocks);
            Assert.Equal(1, blocks.Count);
            Assert.False(operationQueue.Contains(checkpoint));

            await _processor.ProcessEventsAsync(
                context,
                new[] 
                {
                    CreateEventData((byte)'e', 300),
                    CreateEventData((byte)'f', 500),
                });

            Assert.NotNull(blocks);
            Assert.Equal(2, blocks.Count);

            var serializedFrame = Encoding.UTF8.GetString(blocks[0].Frame, 0, blocks[0].FrameLength);
            var lines = serializedFrame.Split(new[] { ColdStorageProcessor.EventDelimiter }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(3, lines.Length);

            Assert.Equal(new string('a', 100), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[0])).Payload);
            Assert.Equal(new string('b', 200), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[1])).Payload);
            Assert.Equal(new string('c', 300), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[2])).Payload);

            serializedFrame = Encoding.UTF8.GetString(blocks[1].Frame, 0, blocks[1].FrameLength);
            lines = serializedFrame.Split(new[] { ColdStorageProcessor.EventDelimiter }, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(2, lines.Length);

            Assert.Equal(new string('d', 400), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[0])).Payload);
            Assert.Equal(new string('e', 300), (JsonConvert.DeserializeObject<ColdStorageEvent>(lines[1])).Payload);
        }

        [Fact]
        [Trait("Running time", "Short")]
        [Trait("Category", "Unit")]
        public async Task WhenUnableToCheckpointWithStorageExceptionThenLogs()
        {
            // Arrange
            var context = MockPartitionContext.Create("0", () =>
                {
                    throw new StorageException();
                });
            
            var processor =
               new ColdStorageProcessor(
                   n => _writerMock.Object,
                   CircuitBreakerWarningLevel,
                   CircuitBreakerStallLevel,
                   CircuitBreakerStallInterval,
                   TimeSpan.FromSeconds(200),
                   "Test",
                   maxBlockSize: MaxBlockSize);

            _writerMock
                .Setup(w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()))
                .Returns(() => TaskHelpers.CreateCompletedTask(true));
                
            await processor.OpenAsync(context);

            // Act
            await processor.ProcessEventsAsync(context, new[] 
                {
                    CreateEventData((byte)'a', 100),
                    CreateEventData((byte)'b', 200),
                    CreateEventData((byte)'c', 300),
                    CreateEventData((byte)'d', 400),
                });
        }

        [Fact]
        [Trait("Running time", "Short")]
        [Trait("Category", "Unit")]
        public async Task WhenUnableToCheckpointWithExceptionThenThrows()
        {
            // Arrange
            var context = MockPartitionContext.Create("0", () =>
            {
                throw new InvalidOperationException();
            });

            _writerMock
                .Setup(w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()))
                .Returns(() => TaskHelpers.CreateCompletedTask(true));

            await _processor.OpenAsync(context);

            await AssertExt.ThrowsAsync<InvalidOperationException>(() => _processor.ProcessEventsAsync(context, new[] 
                {
                    CreateEventData((byte)'a', 100),
                    CreateEventData((byte)'b', 200),
                    CreateEventData((byte)'c', 300),
                    CreateEventData((byte)'d', 400),
                }));
        }

        private static EventData CreateEventData(byte content, int length)
        {
            var eventData = new EventData(Enumerable.Range(0, length).Select(_ => content).ToArray());
            eventData.Properties[EventDataPropertyKeys.DeviceId] = "0";

            return eventData;
        }
    }
}
