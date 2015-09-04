using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.Monitoring.EventProcessor;
using Microsoft.WindowsAzure.Storage.Blob;
using Xunit;

namespace Monitoring.EventProcessor.Tests
{
    public class The_checkpoint_manager
    {
        public const string PartitionId = "0";
        public const string ConsumerGroupName = "MyConsumer";

        private static CloudBlobContainer GetCloudBlobContainerStub()
        {
            return new CloudBlobContainer(new Uri("http://microsoft.com"));
        }

        private static string GetBlobName(string partitionId)
        {
            return $"{ConsumerGroupName}/{partitionId}";
        }

        [Fact]
        public async Task should_check_the_last_modified_date_of_the_blob()
        {
            var dateWasChecked = false;

            Func<CloudBlockBlob, Task<DateTimeOffset>> getLastModified = _ =>
            {
                dateWasChecked = true;
                return Task.FromResult(new DateTimeOffset());
            };

            var manager = new PartitionCheckpointManager(
                ConsumerGroupName,
                new[] { PartitionId },
                GetCloudBlobContainerStub(),
                getLastModified,
                _ => Task.FromResult(new PartitionCheckpoint()));

            await manager.GetLastCheckpointAsync(PartitionId);

            Assert.True(dateWasChecked);
        }

        [Fact]
        public async Task should_retrieve_the_checkpoint()
        {
            var wasRetrieved = false;

            Func<CloudBlockBlob, Task<PartitionCheckpoint>> getCheckpoint = _ =>
            {
                wasRetrieved = true;
                return Task.FromResult(new PartitionCheckpoint());
            };

            var manager = new PartitionCheckpointManager(
                ConsumerGroupName,
                new[] { PartitionId },
                GetCloudBlobContainerStub(),
                _ => Task.FromResult(new DateTimeOffset()),
                getCheckpoint);

            await manager.GetLastCheckpointAsync(PartitionId);

            Assert.True(wasRetrieved);
        }

        public class when_looking_up_the_blob_reference_for_a_partition
        {
            private CloudBlockBlob _blobForLastModified;
            private CloudBlockBlob _blobForRetrieval;

            public when_looking_up_the_blob_reference_for_a_partition()
            {
                var manager = new PartitionCheckpointManager(
                    ConsumerGroupName,
                    new[] { PartitionId },
                    GetCloudBlobContainerStub(),
                    b =>
                    {
                        _blobForLastModified = b;
                        return Task.FromResult(new DateTimeOffset());
                    },
                    b =>
                    {
                        _blobForRetrieval = b;
                        return Task.FromResult(new PartitionCheckpoint());
                    });

                manager.GetLastCheckpointAsync(PartitionId).Wait();
            }

            [Fact]
            public void should_return_a_blob_for_last_modified_that_matches_the_consumer_group_and_partition()
            {
                Assert.NotNull(_blobForLastModified);
                Assert.Equal($"{ConsumerGroupName}/{PartitionId}", _blobForLastModified.Name);
            }

            [Fact]
            public void should_return_a_blob_for_retrieving_that_matches_the_consumer_group_and_partition()
            {
                Assert.NotNull(_blobForRetrieval);
                Assert.Equal($"{ConsumerGroupName}/{PartitionId}", _blobForLastModified.Name);
            }
        }

        public class when_the_last_modified_has_not_been_updated
        {
            [Fact]
            public async Task should_not_retrieve_the_checkpoint()
            {
                var timesRetrieved = 0;
                var lastModified = new DateTimeOffset();
                Func<CloudBlockBlob, Task<DateTimeOffset>> getLastModified = _ => Task.FromResult(lastModified);
                Func<CloudBlockBlob, Task<PartitionCheckpoint>> getCheckpoint = _ =>
                {
                    timesRetrieved++;
                    return Task.FromResult(new PartitionCheckpoint());
                };

                var manager = new PartitionCheckpointManager(
                    ConsumerGroupName,
                    new[] { PartitionId },
                    GetCloudBlobContainerStub(),
                    getLastModified,
                    getCheckpoint);

                await manager.GetLastCheckpointAsync(PartitionId);
                // since the last modified stamp hasn't changed, 
                // we should not attempt to retrieve the blob
                await manager.GetLastCheckpointAsync(PartitionId);

                Assert.Equal(1, timesRetrieved);
            }

            [Fact]
            public async Task should_return_a_cached_checkpoint()
            {
                var lastModified = new DateTimeOffset();
                Func<CloudBlockBlob, Task<DateTimeOffset>> getLastModified = _ => Task.FromResult(lastModified);
                Func<CloudBlockBlob, Task<PartitionCheckpoint>> getCheckpoint = _ => Task.FromResult(new PartitionCheckpoint());

                var manager = new PartitionCheckpointManager(
                    ConsumerGroupName,
                    new[] { PartitionId },
                    GetCloudBlobContainerStub(),
                    getLastModified,
                    getCheckpoint);

                var checkpoint1 = await manager.GetLastCheckpointAsync(PartitionId);
                var checkpoint2 = await manager.GetLastCheckpointAsync(PartitionId);

                Assert.Equal(checkpoint1, checkpoint2);
            }
        }

        public class when_asked_for_an_unknown_parttion
        {
            [Fact]
            public async Task should_throw()
            {
                var manager = new PartitionCheckpointManager(
                    ConsumerGroupName,
                    new string[] { },
                    GetCloudBlobContainerStub(),
                    _ => Task.FromResult(new DateTimeOffset()),
                    _ => Task.FromResult(new PartitionCheckpoint()));

                await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
                 {
                     await manager.GetLastCheckpointAsync("unknown");
                 });
            }
        }

        public class when_accessing_multiple_partitions
        {
            private readonly string[] _partitionIds = { "0", "1", "2" };
            private readonly Dictionary<string, int> _retrievalCounts = new Dictionary<string, int>();
            private readonly PartitionCheckpointManager _manager;

            public when_accessing_multiple_partitions()
            {
                // create a set of differing  "last modified dates"
                var lastModifiedDates = _partitionIds
                    .Select((id, index) => new { id, index })
                    .ToDictionary(
                        x => GetBlobName(x.id),
                        x => new DateTimeOffset().AddDays(x.index));

                // keep track of how many times we attempt to retrieve
                // the checkpoint for each partition
                _retrievalCounts = _partitionIds
                    .ToDictionary(
                        GetBlobName,
                        id => 0);

                _manager = new PartitionCheckpointManager(
                    ConsumerGroupName,
                    _partitionIds,
                    GetCloudBlobContainerStub(),
                    blob => Task.FromResult(lastModifiedDates[blob.Name]),
                    blob =>
                    {
                        _retrievalCounts[blob.Name]++;
                        return Task.FromResult(new PartitionCheckpoint());
                    });
            }

            [Fact]
            public async Task should_return_corresponding_checkpoint()
            {
                var checkpoint1 = await _manager.GetLastCheckpointAsync("0");
                var checkpoint2 = await _manager.GetLastCheckpointAsync("1");
                var checkpoint3 = await _manager.GetLastCheckpointAsync("0");

                Assert.NotEqual(checkpoint2, checkpoint3);
                Assert.Equal(checkpoint1, checkpoint3);
            }

            [Fact]
            public async Task should_cache_each_partition_individually()
            {
                await _manager.GetLastCheckpointAsync("0");
                await _manager.GetLastCheckpointAsync("1");
                await _manager.GetLastCheckpointAsync("0");

                Assert.Equal(1, _retrievalCounts[GetBlobName("0")]);
                Assert.Equal(1, _retrievalCounts[GetBlobName("1")]);
                Assert.Equal(0, _retrievalCounts[GetBlobName("2")]);
            }
        }
    }
}