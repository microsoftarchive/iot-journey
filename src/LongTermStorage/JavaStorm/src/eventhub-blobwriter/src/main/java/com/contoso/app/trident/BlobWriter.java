// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
package com.contoso.app.trident;

import java.io.ByteArrayInputStream;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import backtype.storm.topology.FailedException;

import com.microsoft.azure.storage.AccessCondition;
import com.microsoft.azure.storage.CloudStorageAccount;
import com.microsoft.azure.storage.blob.BlobRequestOptions;
import com.microsoft.azure.storage.blob.BlockEntry;
import com.microsoft.azure.storage.blob.BlockListingFilter;
import com.microsoft.azure.storage.blob.BlockSearchMode;
import com.microsoft.azure.storage.blob.CloudBlobClient;
import com.microsoft.azure.storage.blob.CloudBlobContainer;
import com.microsoft.azure.storage.blob.CloudBlockBlob;
import com.microsoft.azure.storage.core.Base64;

public class BlobWriter {
	private static final Logger logger = (Logger) LoggerFactory.getLogger(BlobWriter.class);
	static CloudBlobContainer container = null;
	static {
		try {
			String containerName = ConfigProperties.getProperty("storage.blob.account.container") + BlobWriterTopology.topologyStartTime;
			String accountName = ConfigProperties.getProperty("storage.blob.account.name");
			String accountKey = ConfigProperties.getProperty("storage.blob.account.key");
			String connectionStrFormatter = "DefaultEndpointsProtocol=http;AccountName=%s;AccountKey=%s";
			String connectionStr = String.format(connectionStrFormatter, accountName, accountKey);
			CloudStorageAccount account = CloudStorageAccount.parse(String.format(connectionStr, accountName, accountKey));
			CloudBlobClient blobClient = account.createCloudBlobClient();
			container = blobClient.getContainerReference(containerName);
			container.createIfNotExists();
		} catch (Exception e) {
			logger.error(e.getMessage());
			throw new ExceptionInInitializerError(e.getMessage());
		}
	}

	static public void upload(String blobname, String blockIdStr, String data) {
		InputStream stream = null;
		try {
			if (LogSetting.LOG_BLOBWRITER) {
				logger.info("upload Begin");
				logger.info("upload blobname = " + blobname);
				logger.info("upload blockIdStr = " + blockIdStr);
			}
			if (LogSetting.LOG_BLOBWRITERDATA) {
				logger.info("upload data= \r\n" + data);				
			}
			CloudBlockBlob blockBlob = container.getBlockBlobReference(blobname);
			BlobRequestOptions blobOptions = new BlobRequestOptions();
			stream = new ByteArrayInputStream(data.getBytes(StandardCharsets.UTF_8));
			BlockEntry newBlock = new BlockEntry(Base64.encode(blockIdStr.getBytes()), BlockSearchMode.UNCOMMITTED);
			ArrayList<BlockEntry> blocksBeforeUpload = new ArrayList<BlockEntry>();
			if (blockBlob.exists(AccessCondition.generateEmptyCondition(), blobOptions, null)) {
				blocksBeforeUpload = blockBlob.downloadBlockList(BlockListingFilter.COMMITTED, null, blobOptions, null);
			}
			if (LogSetting.LOG_BLOBWRITER) {
				int i = 0;
				String id = null;
				for (BlockEntry e : blocksBeforeUpload) {
					i++;
					id = e.getId();
					logger.info("BlockEntry Before Upload id=" + id + ", Index = " + i);
				}
				if (id != null) {
					logger.info("BlockEntry Before Upload id=" + id + ", Index = " + i + " --last before");
				}
			}
			blockBlob.uploadBlock(newBlock.getId(), stream, -1);
			if (!blocksBeforeUpload.contains(newBlock)) {
				blocksBeforeUpload.add(newBlock);
			}
			if (LogSetting.LOG_BLOBWRITER) {
				int i = 0;
				String id = null;
				for (BlockEntry e : blocksBeforeUpload) {
					i++;
					id = e.getId();
					logger.info("BlockEntry After Upload id=" + id + ", Index = " + i);
				}
				if (id != null) {
					logger.info("BlockEntry After Upload id=" + id + ", Index = " + i + " --last after");
				}
			}
			blockBlob.commitBlockList(blocksBeforeUpload);
		} catch (Exception e) {
			throw new FailedException(e.getMessage());
		} finally {
			if (stream != null) {
				try {
					stream.close();
				} catch (Exception e) {
					logger.error("failed to close the stream that upload to azrue blob");
				}
			}
		}
		if (LogSetting.LOG_BLOBWRITER) {
			logger.info("upload End");
		}
	}
}
