// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.ColdStorage.RollingBlobWriter;
using Microsoft.Practices.IoTJourney.Tests.Common.Helpers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Xunit;

namespace Microsoft.Practices.IoTJourney.ColdStorage.Tests
{
    public class GivenARollingBlobWriter : IDisposable
    {
        private const string Prefix = "prefix/";
        private const int BlocksAllowed = 6;
        private const int BlockSize = 1024 * 1024;

        private readonly CloudStorageAccount _storageAccount;
        private readonly string _containerName;
        private readonly RollingBlobWriter.RollingBlobWriter _sut;

        public GivenARollingBlobWriter()
        {
            _storageAccount = TestUtilities.GetStorageAccount();
            _containerName = "test" + Guid.NewGuid().ToString("N").ToLowerInvariant();

            GetContainer().Create();

            _sut =
                new RollingBlobWriter.RollingBlobWriter(
                    Mock.Of<IBlobNamingStrategy>(ns => ns.GetNamePrefix() == Prefix),
                    _storageAccount,
                    _containerName,
                    rollSizeMb: 1,
                    blocksAllowed: BlocksAllowed,
                    blockSize: BlockSize);
        }

        [Fact]
        [Trait("Running time", "Long")]
        [Trait("Category", "Integration")]
        public async Task WhenWritingSingleBlock_ThenBlockIsWritten()
        {
            var payloadString = new string('a', 50);

            Assert.True(
                await _sut.WriteAsync(
                    new[] 
                    {
                        TestUtilities.CreateBlockData(payloadString, BlockSize)
                    },
                    CancellationToken.None));

            Assert.Equal(
                payloadString,
                await GetContainer()
                        .GetBlockBlobReference(Prefix + "0")
                        .DownloadTextAsync(Encoding.UTF8, AccessCondition.GenerateEmptyCondition(), null, null));
        }

        [Fact]
        [Trait("Running time", "Long")]
        [Trait("Category", "Integration")]
        public async Task WhenWritingMultipleBlocks_ThenBlockAreWrittenInOrder()
        {
            var payloadStringA = new string('a', 50);
            var payloadStringB = new string('b', 50);

            Assert.True(
                await _sut.WriteAsync(
                    new[] 
                    {
                        TestUtilities.CreateBlockData(payloadStringA, BlockSize),
                        TestUtilities.CreateBlockData(payloadStringB, BlockSize)
                    },
                    CancellationToken.None));

            Assert.Equal(
                payloadStringA + payloadStringB,
                await GetContainer()
                        .GetBlockBlobReference(Prefix + "0")
                        .DownloadTextAsync(Encoding.UTF8, AccessCondition.GenerateEmptyCondition(), null, null));
        }

        [Fact]
        [Trait("Running time", "Long")]
        [Trait("Category", "Integration")]
        public async Task WhenIssuingMultipleWriteRequests_ThenBlockAreWrittenInOrder()
        {
            var payloadStringA = new string('a', 50);
            var payloadStringB = new string('b', 50);
            var payloadStringC = new string('c', 50);
            var payloadStringD = new string('d', 50);

            Assert.True(
                await _sut.WriteAsync(
                    new[] 
                    {
                        TestUtilities.CreateBlockData(payloadStringA, BlockSize),
                        TestUtilities.CreateBlockData(payloadStringB, BlockSize)
                    },
                    CancellationToken.None));

            Assert.True(
                await _sut.WriteAsync(
                    new[] 
                    {
                        TestUtilities.CreateBlockData(payloadStringC, BlockSize),
                        TestUtilities.CreateBlockData(payloadStringD, BlockSize)
                    },
                    CancellationToken.None));

            Assert.Equal(
                payloadStringA + payloadStringB + payloadStringC + payloadStringD,
                await GetContainer()
                        .GetBlockBlobReference(Prefix + "0")
                        .DownloadTextAsync(Encoding.UTF8, AccessCondition.GenerateEmptyCondition(), null, null));
        }

        [Fact]
        [Trait("Running time", "Long")]
        [Trait("Category", "Integration")]
        public async Task WhenNewRequestGoesOverMaximumBlockLimit_TheWritesNewRequestInNewBlob()
        {
            var payloadStringA = new string('a', 50);
            var payloadStringB = new string('b', 50);
            var payloadStringC = new string('c', 50);
            var payloadStringD = new string('d', 50);

            Assert.True(
                await _sut.WriteAsync(
                                new[] 
                                {
                                    TestUtilities.CreateBlockData(payloadStringA, BlockSize),
                                    TestUtilities.CreateBlockData(payloadStringB, BlockSize),
                                    TestUtilities.CreateBlockData(payloadStringC, BlockSize),
                                    TestUtilities.CreateBlockData(payloadStringD, BlockSize)
                                },
                                CancellationToken.None));

            Assert.True(
                await _sut.WriteAsync(
                    new[] 
                    {
                        TestUtilities.CreateBlockData(payloadStringB, BlockSize),
                        TestUtilities.CreateBlockData(payloadStringC, BlockSize),
                        TestUtilities.CreateBlockData(payloadStringD, BlockSize)
                    },
                    CancellationToken.None));

            Assert.Equal(
                payloadStringA + payloadStringB + payloadStringC + payloadStringD,
                await GetContainer()
                        .GetBlockBlobReference(Prefix + "0")
                        .DownloadTextAsync(Encoding.UTF8, AccessCondition.GenerateEmptyCondition(), null, null));

            Assert.Equal(
                payloadStringB + payloadStringC + payloadStringD,
                await GetContainer()
                        .GetBlockBlobReference(Prefix + "1")
                        .DownloadTextAsync(Encoding.UTF8, AccessCondition.GenerateEmptyCondition(), null, null));
        }

        [Fact]
        [Trait("Running time", "Long")]
        [Trait("Category", "Integration")]
        public async Task WhenNewRequestGoesOverRollingSizeLimit_TheWritesNewRequestInNewBlob()
        {
            var payloadStringA = new string('a', 50);
            var payloadStringB = new string('b', 50);
            var payloadStringC = new string('c', 50);
            var payloadStringD = new string('d', 50);
            var longPayloadString = new string('e', BlockSize - 10);

            Assert.True(
                await _sut.WriteAsync(
                    new[] 
                    {
                        TestUtilities.CreateBlockData(payloadStringA, BlockSize),
                        TestUtilities.CreateBlockData(payloadStringB, BlockSize),
                        TestUtilities.CreateBlockData(payloadStringC, BlockSize),
                        TestUtilities.CreateBlockData(payloadStringD, BlockSize)
                    },
                    CancellationToken.None));

            Assert.True(
                await _sut.WriteAsync(
                    new[] 
                    {
                        TestUtilities.CreateBlockData(longPayloadString, BlockSize),
                    },
                    CancellationToken.None));

            Assert.Equal(
                payloadStringA + payloadStringB + payloadStringC + payloadStringD,
                await GetContainer()
                        .GetBlockBlobReference(Prefix + "0")
                        .DownloadTextAsync(Encoding.UTF8, AccessCondition.GenerateEmptyCondition(), null, null));

            Assert.Equal(
                longPayloadString,
                await GetContainer()
                        .GetBlockBlobReference(Prefix + "1")
                        .DownloadTextAsync(Encoding.UTF8, AccessCondition.GenerateEmptyCondition(), null, null));
        }

        [Fact]
        [Trait("Running time", "Long")]
        [Trait("Category", "Integration")]
        public async Task WhenNewRequestFindsUpdatedBlob_ThenRequestReturnsFalse()
        {
            var payloadStringA = new string('a', 50);
            var payloadStringB = new string('b', 50);
            var payloadStringC = new string('c', 50);
            var payloadStringD = new string('d', 50);

            Assert.True(
                await _sut.WriteAsync(
                    new[] 
                    {
                        TestUtilities.CreateBlockData(payloadStringA, BlockSize),
                        TestUtilities.CreateBlockData(payloadStringB, BlockSize)
                    },
                    CancellationToken.None));

            await AzureStorageHelpers.TouchBlobAsync(GetContainer().GetBlockBlobReference(Prefix + "0"));

            Assert.False(
                await _sut.WriteAsync(
                    new[] 
                    {
                        TestUtilities.CreateBlockData(payloadStringC, BlockSize),
                        TestUtilities.CreateBlockData(payloadStringD, BlockSize)
                    },
                    CancellationToken.None));

            Assert.Equal(
                payloadStringA + payloadStringB,
                await GetContainer()
                        .GetBlockBlobReference(Prefix + "0")
                        .DownloadTextAsync(Encoding.UTF8, AccessCondition.GenerateEmptyCondition(), null, null));
        }

        [Fact]
        [Trait("Running time", "Long")]
        [Trait("Category", "Integration")]
        public async Task WhenSendingNewRequestAfterExceptionForConcurrency_ThenWriterIsResetAndWritesResentData()
        {
            var payloadStringA = new string('a', 50);
            var payloadStringB = new string('b', 50);
            var payloadStringC = new string('c', 50);
            var payloadStringD = new string('d', 50);

            Assert.True(
                await _sut.WriteAsync(
                    new[] 
                    {
                        TestUtilities.CreateBlockData(payloadStringA, BlockSize),
                        TestUtilities.CreateBlockData(payloadStringB, BlockSize)
                    },
                    CancellationToken.None));

            await AzureStorageHelpers.TouchBlobAsync(GetContainer().GetBlockBlobReference(Prefix + "0"));

            Assert.False(
                await _sut.WriteAsync(
                    new[] 
                    {
                        TestUtilities.CreateBlockData(payloadStringC, BlockSize),
                        TestUtilities.CreateBlockData(payloadStringD, BlockSize)
                    },
                    CancellationToken.None));

            Assert.True(
                await _sut.WriteAsync(
                    new[] 
                            {
                                TestUtilities.CreateBlockData(payloadStringC, BlockSize),
                                TestUtilities.CreateBlockData(payloadStringD, BlockSize),
                                TestUtilities.CreateBlockData(payloadStringB, BlockSize)
                            },
                    CancellationToken.None));

            Assert.Equal(
                payloadStringA + payloadStringB + payloadStringC + payloadStringD + payloadStringB,
                await GetContainer()
                        .GetBlockBlobReference(Prefix + "0")
                        .DownloadTextAsync(Encoding.UTF8, AccessCondition.GenerateEmptyCondition(), null, null));
        }

        public void Dispose()
        {
            GetContainer().DeleteIfExists();
        }

        private CloudBlobContainer GetContainer()
        {
            var client = _storageAccount.CreateCloudBlobClient();
            return client.GetContainerReference(_containerName);
        }
    }
}