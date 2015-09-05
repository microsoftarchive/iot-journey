using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor
{
    public class PartitionCheckpointManager
    {
        private readonly string _consumerGroupName;
        private readonly CloudBlobContainer _blobContainer;
        private readonly Func<CloudBlockBlob, Task<DateTimeOffset>> _getLastModified;
        private readonly Func<CloudBlockBlob, Task<PartitionCheckpoint>> _getCheckpoint;
        private readonly Dictionary<string, PartitionCheckpoint> _checkpoints = new Dictionary<string, PartitionCheckpoint>();
        private readonly Dictionary<string, DateTimeOffset> _timestamps = new Dictionary<string, DateTimeOffset>();
        private Dictionary<string, CloudBlockBlob> _blobs = new Dictionary<string, CloudBlockBlob>();

        public PartitionCheckpointManager(
            string consumerGroupName,
            IEnumerable<string> partitionIds,
            CloudBlobContainer blobContainer,
            Func<CloudBlockBlob, Task<DateTimeOffset>> getLastModified = null,
            Func<CloudBlockBlob, Task<PartitionCheckpoint>> getCheckpoint = null)
        {
            _consumerGroupName = consumerGroupName;

            _blobContainer = blobContainer;
            _getLastModified = getLastModified ?? GetLastModifiedAsync;
            _getCheckpoint = getCheckpoint ?? GetCheckpointAsync;

            RegisterPartitions(partitionIds);
        }

        private static async Task<DateTimeOffset> GetLastModifiedAsync(CloudBlockBlob blob)
        {
            await blob.FetchAttributesAsync().ConfigureAwait(false);
            return blob.Properties.LastModified.Value;
        }

        private static async Task<PartitionCheckpoint> GetCheckpointAsync(CloudBlockBlob blob)
        {
            var text = await blob.DownloadTextAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<PartitionCheckpoint>(text);
        }

        public async Task<PartitionCheckpoint> GetLastCheckpointAsync(string partitionId)
        {
            if(!_blobs.ContainsKey(partitionId)) throw new ArgumentException(
                $"The partition id {partitionId} is unknown. Has it been registered?");

            var blob = _blobs[partitionId];

            var latestTimestamp = await _getLastModified(blob);
            if (_timestamps.ContainsKey(partitionId) 
                && _timestamps[partitionId] == latestTimestamp) return _checkpoints[partitionId];

            _timestamps[partitionId] = latestTimestamp;
            var checkpoint = await _getCheckpoint(blob);
            checkpoint.LastCheckpointTimeUtc = latestTimestamp;

            _checkpoints[partitionId] = checkpoint;

            return checkpoint;
        }

        private void RegisterPartitions(IEnumerable<string> partitionIds)
        {
            _blobs = partitionIds.ToDictionary(
                id => id,
                id =>
                {
                    var blobName = _consumerGroupName + "/" + id;
                    return _blobContainer.GetBlockBlobReference(blobName);
                });
        }
    }
}