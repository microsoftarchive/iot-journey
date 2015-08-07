// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

package com.contoso.app.trident;

import java.util.Map;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import backtype.storm.topology.FailedException;
import backtype.storm.tuple.Values;
import storm.trident.operation.BaseAggregator;
import storm.trident.operation.TridentCollector;
import storm.trident.operation.TridentOperationContext;
import storm.trident.topology.TransactionAttempt;
import storm.trident.tuple.TridentTuple;

public class ByteAggregator extends BaseAggregator<BlockState> {
	private static final long serialVersionUID = 1L;
	private static final Logger logger = (Logger) LoggerFactory.getLogger(ByteAggregator.class);
	private static String txidKeyFormatter = "partition_%05d_transactionid";
	private static String firstblockKeyFormatter = "partition_%05d_firstblock";
	private static String lastblockKeyFormatter = "partition_%05d_lastblock";

	public long txid;
	public int partitionIndex;
	private long msgCount;
	public String txidKey = null;
	public String firstblockKey = null;
	public String lastblockKey = null;
	boolean needPersist = false;

	static {
		String txidKeyFormatterStr = ConfigProperties.getProperty("PARTITION_TXID_KEY_FORMATTER");
		if (txidKeyFormatterStr != null) {
			txidKeyFormatter = txidKeyFormatterStr;
		}
		String firstblockKeyFormatterStr = ConfigProperties.getProperty("PARTITION_FIRSTBLOCK_KEY_FORMATTER");
		if (firstblockKeyFormatterStr != null) {
			firstblockKeyFormatter = firstblockKeyFormatterStr;
		}
		String lastblockKeyFormatterStr = ConfigProperties.getProperty("PARTITION_LASTBLOCK_KEY_FORMATTER");
		if (lastblockKeyFormatterStr != null) {
			lastblockKeyFormatter = lastblockKeyFormatterStr;
		}
	}

	public ByteAggregator() {
		if (LogSetting.LOG_BATCH) {
			logger.info("Constructor");
		}
	}

	@Override
	public void prepare(@SuppressWarnings("rawtypes") Map conf, TridentOperationContext context) {
		if (LogSetting.LOG_BATCH) {
			logger.info("prepare Begin");
		}
		partitionIndex = context.getPartitionIndex();
		txidKey = String.format(txidKeyFormatter, partitionIndex);
		firstblockKey = String.format(firstblockKeyFormatter, partitionIndex);
		lastblockKey = String.format(lastblockKeyFormatter, partitionIndex);
		BlockStateStore.clearState(this);
		super.prepare(conf, context);
		if (LogSetting.LOG_BATCH) {
			logger.info("p" + partitionIndex + ": prepare End");
		}
	}

	public BlockState init(Object batchId, TridentCollector collector) {
		if (LogSetting.LOG_BATCH) {
			logger.info("p" + partitionIndex + ": init End");
		}
		if (batchId instanceof TransactionAttempt) {
			txid = ((TransactionAttempt) batchId).getTransactionId();
		} else {
			throw new FailedException("Error configuring ByteAggregator");
		}
		msgCount = 0;
		needPersist = false;
		BlockState blockState = new BlockState(this);
		if (LogSetting.LOG_BATCH) {
			logger.info(blockState.partitionTxidLogStr + "init End");
		}
		return blockState;
	}

	public void aggregate(BlockState blockState, TridentTuple tuple, TridentCollector collector) {
		if (LogSetting.LOG_MESSAGE) {
			logger.info(blockState.partitionTxidLogStr + "aggregate Begin");
		}
		String tupleStr = tuple.getString(0);
		if (tupleStr != null && tupleStr.length() > 0) {
			if (LogSetting.LOG_MESSAGE) {
				logger.info(blockState.partitionTxidLogStr + "Message= " + tupleStr);
			}
			String msg = tupleStr + "\r\n";
			if (Block.isMessageSizeWithnLimit(msg)) {
				if (blockState.currentBlock.willMessageFitCurrentBlock(msg)) {
					blockState.currentBlock.addData(msg);
				} else {
					// since the new msg will not fit into the current block, we will upload the current block,
					// and then get the next block, and add the new msg to the next block
					blockState.currentBlock.upload(partitionIndex);
					needPersist = true;
					if (LogSetting.LOG_MESSAGEROLLOVER) {
						logger.info(blockState.partitionTxidLogStr + " Message does not fit current block; rollover to next block");
					}
					blockState.currentBlock = blockState.getNextBlock(blockState.currentBlock);
					blockState.currentBlock.addData(msg);
				}
				msgCount++;
			} else {
				// message size is not within the limit, skip the message and log it.
				logger.error(blockState.partitionTxidLogStr + "message skiped: message size exceeds the size limit, message= " + tupleStr);
			}
		}
		if (LogSetting.LOG_MESSAGE) {
			logger.info(blockState.partitionTxidLogStr + "aggregate End");
		}
	}

	public void complete(BlockState blockState, TridentCollector collector) {
		if (LogSetting.LOG_BATCH) {
			logger.info(blockState.partitionTxidLogStr + "complete Begin");
		}
		if (blockState.currentBlock.blockdataSize > 0) {
			blockState.currentBlock.upload(partitionIndex);
			needPersist = true;
		}
		if (needPersist) {
			blockState.persistState();
		}
		collector.emit(new Values(msgCount));
		if (LogSetting.LOG_BATCH) {
			logger.info(blockState.partitionTxidLogStr + "message count = " + msgCount);
			logger.info(blockState.partitionTxidLogStr + "complete End");
		}
	}
}
