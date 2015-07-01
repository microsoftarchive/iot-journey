// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.ColdStorage.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;

namespace Microsoft.Practices.IoTJourney.ColdStorage.RollingBlobWriter
{
    public class RollingBlobWriter : IBlobWriter
    {
        private const int MegaBytes = 1024 * 1024;
        private const int MaxBlocksAllowed = 50000;
        internal const int MaxBlockSize = 4 * MegaBytes;

        private readonly CloudBlobClient _blobClient;
        private readonly string _containerName;
        private readonly long _rollSizeBytes;
        private readonly IBlobNamingStrategy _namingStrategy;
        private readonly int _blocksAllowed;
        private readonly int _blockSize;

        private string _currentBlobPrefix = null;
        private int _currentSequenceId = 0;
        private CloudBlockBlob _currentBlob = null;

        private IReadOnlyCollection<string> _blockIds = null;
        private long _sizeRemaining = 0;
        private string _currentBlobEtag;

        public RollingBlobWriter(IBlobNamingStrategy namingStrategy,
            CloudStorageAccount storageAccount,
            string containerName,
            int rollSizeMb,
            int blocksAllowed = MaxBlocksAllowed,
            int blockSize = MaxBlockSize)
        {
            Guard.ArgumentNotNullOrEmpty(containerName, "containerName");
            Guard.ArgumentGreaterOrEqualThan(1, blocksAllowed, "blocksAllowed");
            Guard.ArgumentLowerOrEqualThan(MaxBlocksAllowed, blocksAllowed, "blocksAllowed");
            Guard.ArgumentGreaterOrEqualThan(1, blockSize, "blockSize");
            Guard.ArgumentLowerOrEqualThan(MaxBlockSize, blockSize, "blockSize");
            Guard.ArgumentGreaterOrEqualThan(1, rollSizeMb, "rollSizeMb");

            _rollSizeBytes = rollSizeMb * (long)MegaBytes;
            Guard.ArgumentLowerOrEqualThan(blocksAllowed * (long)blockSize, _rollSizeBytes, "rollSizeMb");

            _blobClient = storageAccount.CreateCloudBlobClient();
            _containerName = containerName;
            _namingStrategy = namingStrategy;
            _blocksAllowed = blocksAllowed;
            _blockSize = blockSize;
        }

        public async Task<bool> WriteAsync(IReadOnlyCollection<BlockData> blocksToWrite, CancellationToken cancellationToken)
        {
            Guard.ArgumentNotNull(blocksToWrite, "blocksToWrite");
            var totalBytesToWrite = ValidateBlockDataList(blocksToWrite);

            var writeTimeStopWatch = Stopwatch.StartNew();

            if (!await SetupBlobForWriteAsync(totalBytesToWrite, blocksToWrite.Count, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            ColdStorageEventSource.Log.WriteToBlobStarted(_currentBlob.Name, _blockIds.Count, blocksToWrite.Count, totalBytesToWrite);

            var taskList = new List<Task>();
            var newBlockIds = new List<String>(_blockIds);
            foreach (var block in blocksToWrite)
            {
                string newId = null;
                newId = Convert.ToBase64String(BitConverter.GetBytes(newBlockIds.Count));
                newBlockIds.Add(newId);
                taskList.Add(this.WriteBlockAsync(newId, block.Frame, 0, block.FrameLength, cancellationToken));
            }

            try
            {
                await Task.WhenAll(taskList).ConfigureAwait(false).ConfigureAwait(false);
            }
            catch (AggregateException ae)
            {
                ColdStorageEventSource.Log.WriteToBlobFailed(ae, _currentBlob.Name, blocksToWrite.Count, totalBytesToWrite);

                if (HandleExceptionsWritingBlocks(ae))
                {
                    return false;
                }

                throw;
            }

            try
            {
                var accessCondition =
                    _currentBlobEtag != null
                        ? AccessCondition.GenerateIfMatchCondition(_currentBlobEtag)
                        : AccessCondition.GenerateIfNoneMatchCondition("*");

                await _currentBlob
                    .PutBlockListAsync(newBlockIds, accessCondition, null, null, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                {
                    ColdStorageEventSource.Log.BlobEtagMissMatchOccured(_currentBlob.Name);
                    ResetCurrentWriterState();
                }
                else if (IsHardStorageException(ex))
                {
                    ColdStorageEventSource.Log.HardStorageExceptionCaughtWritingToBlob(ex, _blobClient, _containerName);
                }
                else
                {
                    ColdStorageEventSource.Log.StorageExceptionCaughtWritingToBlob(ex, _blobClient, _containerName);
                }

                return false;
            }

            writeTimeStopWatch.Stop();

            // The blob has been commited, update the cached state
            _blockIds = newBlockIds;
            _sizeRemaining = _sizeRemaining - totalBytesToWrite;
            _currentBlobEtag = _currentBlob.Properties.ETag;

            ColdStorageEventSource.Log.WriteToBlobEnded(_currentBlob.Name, _blockIds.Count);

            return true;
        }

        private long ValidateBlockDataList(IReadOnlyCollection<BlockData> blocksToWrite)
        {
            if (blocksToWrite.Count > _blocksAllowed)
            {
                throw new ArgumentOutOfRangeException("blocksToWrite", "Too many blocks");
            }

            long totalWriteSize = 0L;
            foreach (var item in blocksToWrite)
            {
                if (item == null)
                {
                    throw new ArgumentNullException("blocksToWrite", "No item in blockDataList");
                }

                if (item.FrameLength > _blockSize)
                {
                    throw new ArgumentOutOfRangeException("blocksToWrite", "The actual frame length is greater than the block size");
                }

                totalWriteSize += item.FrameLength;
            }

            if (totalWriteSize > _rollSizeBytes)
            {
                throw new ArgumentOutOfRangeException("blocksToWrite", "Total write size is larger than the roll size");
            }

            return totalWriteSize;
        }

        private async Task<bool> SetupBlobForWriteAsync(long sizeToWrite, int numberOfBlocks, CancellationToken cancellationToken)
        {
            string prefix = _namingStrategy.GetNamePrefix();

            try
            {
                if (_currentBlob == null)
                {
                    await this.LoadLastBlobWithPrefixAsync(prefix, cancellationToken).ConfigureAwait(false);
                }

                await CheckAndRollIfNeededAsync(prefix, sizeToWrite, numberOfBlocks, cancellationToken).ConfigureAwait(false);
            }
            catch (StorageException ex)
            {
                if (IsHardStorageException(ex))
                {
                    ColdStorageEventSource.Log.HardStorageExceptionCaughtWritingToBlob(ex, _blobClient, _containerName);
                }
                else
                {
                    ColdStorageEventSource.Log.StorageExceptionCaughtWritingToBlob(ex, _blobClient, _containerName);
                }

                return false;
            }

            return true;
        }

        private async Task<bool> LoadLastBlobWithPrefixAsync(string blobNamePrefix, CancellationToken cancellationToken)
        {
            var container = _blobClient.GetContainerReference(_containerName);
            var existingBlobs = new List<CloudBlockBlob>();
            BlobResultSegment segment = null;

            while (segment == null || segment.ContinuationToken != null)
            {
                segment =
                    await
                        container.ListBlobsSegmentedAsync(blobNamePrefix, false, BlobListingDetails.Metadata, null,
                            segment != null ? segment.ContinuationToken : null, null, null, CancellationToken.None);

                existingBlobs.AddRange(segment.Results.OfType<CloudBlockBlob>());
            }

            DateTimeOffset? lastUpdated = null;
            CloudBlockBlob lastBlob = null;

            foreach (var existingBlob in existingBlobs)
            {
                // TODO shouldn't this be based on sequence rather than date?
                if (lastUpdated == null || existingBlob.Properties.LastModified > lastUpdated)
                {
                    lastBlob = existingBlob;
                    lastUpdated = existingBlob.Properties.LastModified;
                }
            }

            if (lastBlob != null)
            {
                string blobName = lastBlob.Name;
                string[] nameParts = blobName.Split('-');
                string sequenceIdPart = nameParts.Last();
                var sequenceId = Int32.Parse(sequenceIdPart, CultureInfo.InvariantCulture);
                return await SetupBlockCurrentBlobAsync(blobNamePrefix, sequenceId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await SetupBlockCurrentBlobAsync(blobNamePrefix, 0, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task CheckAndRollIfNeededAsync(string prefix, long sizeToWrite, int numChunks, CancellationToken cancellationToken)
        {
            string currentBlobName = _currentBlob.Name;

            if (_currentBlobPrefix != prefix)
            {
                await LoadLastBlobWithPrefixAsync(prefix, cancellationToken).ConfigureAwait(false);
            }

            while ((_sizeRemaining < sizeToWrite) || (_blockIds.Count + numChunks > _blocksAllowed))
            {
                await SetupBlockCurrentBlobAsync(prefix, _currentSequenceId + 1, cancellationToken).ConfigureAwait(false);

                ColdStorageEventSource.Log.RollOccured(currentBlobName, _currentBlob.Name);
            }
        }

        private async Task<bool> SetupBlockCurrentBlobAsync(string blobNamePrefix, int sequenceId, CancellationToken cancellationToken)
        {
            var blockBlob = GetBlob(blobNamePrefix, sequenceId);
            var blockIds = new List<string>();
            var sizeRemaining = _rollSizeBytes;
            string blobEtag = null;
            bool isNewBlob = true;

            if (await blockBlob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                var items =
                    await
                        blockBlob.DownloadBlockListAsync(
                            BlockListingFilter.Committed,
                            AccessCondition.GenerateEmptyCondition(),
                            new BlobRequestOptions(),
                            null,
                            cancellationToken);
                blockIds.AddRange(items.Select(b => b.Name));

                sizeRemaining -= blockBlob.Properties.Length;
                blobEtag = blockBlob.Properties.ETag;

                isNewBlob = false;
            }

            InitializeCurrentWriterState(blobNamePrefix, sequenceId, blockBlob, blockIds, sizeRemaining, blobEtag);

            return isNewBlob;
        }

        private CloudBlockBlob GetBlob(string prefix, int sequenceId)
        {
            var container = _blobClient.GetContainerReference(_containerName);
            return container.GetBlockBlobReference(prefix + sequenceId.ToString(CultureInfo.InvariantCulture));
        }

        private async Task WriteBlockAsync(string Id, byte[] bytes, int startIndex, int length, CancellationToken cancellationToken)
        {
            using (var ms = new MemoryStream(bytes, startIndex, length))
            {
                // These should be new blocks - use the appropropriate condition
                await _currentBlob.PutBlockAsync(Id, ms, null, AccessCondition.GenerateIfNoneMatchCondition("*"), null, null, cancellationToken).ConfigureAwait(false);
            }
        }

        private void InitializeCurrentWriterState(string blobNamePrefix, int sequenceId, CloudBlockBlob blockBlob, IReadOnlyCollection<string> blockIds, long sizeRemaining, string blobEtag)
        {
            _currentBlobPrefix = blobNamePrefix;
            _currentSequenceId = sequenceId;
            _currentBlob = blockBlob;
            _blockIds = blockIds;
            _sizeRemaining = sizeRemaining;
            _currentBlobEtag = blobEtag;
        }

        private void ResetCurrentWriterState()
        {
            _currentBlobPrefix = null;
            _currentSequenceId = 0;
            _currentBlob = null;
            _blockIds = null;
            _sizeRemaining = 0;
            _currentBlobEtag = null;
        }

        private bool HandleExceptionsWritingBlocks(AggregateException aggregateException)
        {
            var flattenedException = aggregateException.Flatten();

            // deal with "hard" storage exceptions
            // only log one error per ErrorCode
            var representativeHardStorageExceptions =
                flattenedException
                    .InnerExceptions
                    .OfType<StorageException>()
                    .Where(e => IsHardStorageException(e))
                    .GroupBy(se => se.RequestInformation.ExtendedErrorInformation.ErrorCode)
                    .Select(g => g.First());
            foreach (var storageException in representativeHardStorageExceptions)
            {
                ColdStorageEventSource.Log.HardStorageExceptionCaughtWritingToBlob(storageException, _blobClient, _containerName);
            }

            // deal with "soft" storage exceptions
            var softStorageExceptions =
                    flattenedException
                        .InnerExceptions
                        .OfType<StorageException>()
                        .Where(e => !IsHardStorageException(e));
            foreach (var storageException in softStorageExceptions)
            {
                if (storageException.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed)
                {
                    ColdStorageEventSource.Log.BlobEtagMissMatchOccured(_currentBlob.Name);
                    ResetCurrentWriterState();
                }
                else
                {
                    ColdStorageEventSource.Log.StorageExceptionCaughtWritingToBlob(storageException, _blobClient, _containerName);
                }
            }

            // return true if all exceptions are StorageExceptions, which were already handled; otherwise, false.
            return flattenedException.InnerExceptions.All(ex => ex is StorageException);
        }

        private bool IsHardStorageException(StorageException ex)
        {
            var errorCode = ex.RequestInformation.ExtendedErrorInformation.ErrorCode;
            if (errorCode == StorageErrorCodeStrings.ContainerBeingDeleted
                || errorCode == StorageErrorCodeStrings.ContainerDisabled
                || errorCode == StorageErrorCodeStrings.ContainerNotFound
                || errorCode == StorageErrorCodeStrings.AuthenticationFailed
                || errorCode == StorageErrorCodeStrings.AccountIsDisabled)
            {
                return true;
            }

            return false;
        }
    }
}