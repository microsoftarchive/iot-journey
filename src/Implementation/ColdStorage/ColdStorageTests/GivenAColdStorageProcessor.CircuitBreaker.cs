// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.ColdStorage.RollingBlobWriter;
using Microsoft.Practices.IoTJourney.Tests.Common;
using Microsoft.Practices.IoTJourney.Tests.Common.Helpers;
using Microsoft.Practices.IoTJourney.Tests.Common.Mocks;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Practices.IoTJourney.ColdStorage.Tests
{
    partial class GivenAColdStorageProcessor
    {
        private const int CircuitBreakerWarningLevel = 5;
        private const int CircuitBreakerStallLevel = 10;
        private static readonly TimeSpan CircuitBreakerStallInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan TimeoutInterval = TimeSpan.FromSeconds(20);

        [Fact]
        [Trait("Running time", "Short")]
        [Trait("Category", "Unit")]
        public async Task WhenCachedBlocksRemainBelowTripLevel_ThenDoesNotStall()
        {
            var blocks = new List<IList<BlockData>>();
            Action<IReadOnlyCollection<BlockData>, CancellationToken> saveBlocks = (d, ct) => blocks.Add(d.ToList());

            _writerMock
                .Setup(w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()))
                .Returns(() => TaskHelpers.CreateCompletedTask(false))
                .Callback(saveBlocks);

            var context = MockPartitionContext.CreateWithNoopCheckpoint("0");

            await _processor.OpenAsync(context);

            for (int i = 0; i < CircuitBreakerStallLevel; i++)
            {
                var batch = new[] { CreateEventData((byte)('a' + i), MaxBlockSize - 200 - i), };
                Task processTask = _processor.ProcessEventsAsync(context, batch);

                await AssertExt.CompletesBeforeTimeoutAsync(processTask, TimeoutInterval);
            }
        }

        [Fact]
        [Trait("Running time", "Long")]
        [Trait("Category", "Unit")]
        public async Task WhenCachedBlocksHitTripLevel_ThenStallsUntilWriteOperationSucceeds()
        {
            var blocks = new List<IList<BlockData>>();
            Action<IReadOnlyCollection<BlockData>, CancellationToken> saveBlocks = (d, ct) => blocks.Add(d.ToList());

            var failWrite = true;
            _writerMock
                .Setup(w => w.WriteAsync(It.IsAny<IReadOnlyCollection<BlockData>>(), It.IsAny<CancellationToken>()))
                .Returns(() => TaskHelpers.CreateCompletedTask(!Volatile.Read(ref failWrite)))
                .Callback(saveBlocks);

            var context = MockPartitionContext.CreateWithNoopCheckpoint("0");

            await _processor.OpenAsync(context);

            // first N requests fail but go through (the first batch will not fill a frame so it won't result in a write operation)
            for (int i = 0; i < CircuitBreakerStallLevel + 1; i++)
            {
                var batch = new[] { CreateEventData((byte)('a' + i), MaxBlockSize - 200 - i), };
                Task processTask = _processor.ProcessEventsAsync(context, batch);

                await AssertExt.CompletesBeforeTimeoutAsync(processTask, TimeoutInterval);
            }

            // N+1th stalls and waits for cached frames to be flushed
            {
                var batch = new[] { CreateEventData((byte)('a' + CircuitBreakerStallLevel + 2), 10), };
                Task processTask = _processor.ProcessEventsAsync(context, batch);

                await AssertExt.DoesNotCompleteBeforeTimeoutAsync(processTask, TimeoutInterval);

                // let the write operation through
                Volatile.Write(ref failWrite, false);

                await AssertExt.CompletesBeforeTimeoutAsync(processTask, TimeoutInterval);

                // check the stalled entries were written
                var bufferedBlocks = blocks.Last();
                Assert.Equal(CircuitBreakerStallLevel, bufferedBlocks.Count);
                for (int i = 0; i < CircuitBreakerStallLevel; i++)
                {
                    var lines = GetPayloadsFromBlock(bufferedBlocks[i]);

                    Assert.Equal(1, lines.Length);
                    Assert.Equal(
                        new string((char)('a' + i), MaxBlockSize - 200 - i),
                        lines[0]);
                }
            }
        }

        private static dynamic[] GetPayloadsFromBlock(BlockData blockData)
        {
            var serializedFrame = Encoding.UTF8.GetString(blockData.Frame, 0, blockData.FrameLength);
            return serializedFrame
                        .Split(new[] { ColdStorageProcessor.EventDelimiter }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => JsonConvert.DeserializeObject<ColdStorageEvent>(l).Payload)
                        .ToArray();
        }
    }
}