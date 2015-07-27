// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

package com.contoso.app.trident;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class Block {
	private static final Logger logger = (Logger) LoggerFactory.getLogger(Block.class);
	private static String blockidFormatter = "%05d";
	private static String blobidAndblockidFormatter = "%05d_%05d";
	private static String blockNameFormatter = "partition_%05d/blob_%05d";

	private static int maxBlockBytes = 4194304;
	static {
		String blockNameFormatterStr = ConfigProperties.getProperty("BLOBNAME_FORMATTER");
		if (blockNameFormatterStr != null) {
			blockNameFormatter = blockNameFormatterStr;
		}

		String blockidFormatterStr = ConfigProperties.getProperty("BLOCKID_FORMATTER");
		if (blockidFormatterStr != null) {
			blockidFormatter = blockidFormatterStr;
		}
		String blobidAndblockidFormatterStr = ConfigProperties.getProperty("BLOBID_BLOCKID_FORMATTER");
		if (blobidAndblockidFormatterStr != null) {
			blobidAndblockidFormatter = blobidAndblockidFormatterStr;
		}

		String maxBlockBytesStr = ConfigProperties.getProperty("storage.blob.block.bytes.max");
		if (maxBlockBytesStr != null) {
			int maxBlockBytesStrInt = Integer.parseInt(maxBlockBytesStr);
			if (maxBlockBytesStrInt > 0 && maxBlockBytesStrInt <= 4194304) {
				maxBlockBytes = maxBlockBytesStrInt;
			}
		}
	}

	public int blobid;
	public int blockid;
	public StringBuilder blockdata;
	public int blockdataSize; 
	public String blobidAndBlockidStr;

	public Block(int blobid, int blockid) {
		if (LogSetting.LOG_BLOCK) {
			logger.info("Block Constructor Begin");
		}
		this.blobid = blobid;
		this.blockid = blockid;
		blockdata = new StringBuilder(maxBlockBytes);
		blockdataSize = 0;
		blobidAndBlockidStr = String.format(blobidAndblockidFormatter, this.blobid, this.blockid);
		if (LogSetting.LOG_BLOCK) {
			logger.info("Block Constructor End");
		}
	}

	public void addData(String msg) {
		if (LogSetting.LOG_MESSAGE) {
			logger.info("Block.addData Begin");
		}
		blockdata.append(msg);
		blockdataSize += msg.getBytes().length;
		if (LogSetting.LOG_MESSAGE) {
			logger.info("Block.addData End");
		}
	}

	public static boolean isMessageSizeWithnLimit(String msg) {
		if (LogSetting.LOG_MESSAGE) {
			logger.info("Block.isMessageSizeWithnLimit Begin");
		}
		boolean result = false;
		if (msg.getBytes().length <= maxBlockBytes) {
			result = true;
		}
		if (LogSetting.LOG_MESSAGE) {
			logger.info("Block.isMessageSizeWithnLimit End");
		}
		return result;
	}

	public boolean willMessageFitCurrentBlock(String msg) {
		if (LogSetting.LOG_MESSAGE) {
			logger.info("Block.willMessageFitCurrentBlock Begin");
		}
		boolean result = false;
		int newSize = blockdataSize + msg.getBytes().length;
		if (newSize <= maxBlockBytes) {
			result = true;
		}
		if (LogSetting.LOG_MESSAGE) {
			logger.info("Block.willMessageFitCurrentBlock End");
		}
		return result;
	}

	public void upload(int partitionIndex) {
		if (LogSetting.LOG_BLOCK) {
			logger.info("Block.upload Begin");
		}
		String blobname = String.format(blockNameFormatter, partitionIndex, blobid);
		String blockidStr = String.format(blockidFormatter, blockid);
		BlobWriter.upload(blobname, blockidStr, blockdata.toString());
		if (LogSetting.LOG_BLOCK) {
			logger.info("BlobState.upload End");
		}
	}
}