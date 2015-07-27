package com.contoso.app.trident;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import redis.clients.jedis.Jedis;
import redis.clients.jedis.Transaction;
import backtype.storm.topology.FailedException;

public class BlockStateStore {
	// TODO: replace Redis with zookeeper to store state
	static public String get(String key) {
		return Redis.get(key);
	}

	static public void setState(BlockState blockState) {
		Redis.setState(blockState);
	}

	static public void clearState(ByteAggregator byteAggregator) {
		Redis.clearState(byteAggregator);
	}

	private static class Redis {
		private static final Logger logger = (Logger) LoggerFactory.getLogger(Redis.class);
		private static String host = null;
		private static String password = null;
		private static int port = -1;
		private static int timeout = -1;

		static {
			host = ConfigProperties.getProperty("redis.host");
			password = ConfigProperties.getProperty("redis.password");
			port = Integer.parseInt(ConfigProperties.getProperty("redis.port"));
			timeout = Integer.parseInt(ConfigProperties.getProperty("redis.timeout"));
			if (host == null) {
				throw new ExceptionInInitializerError("Error: host is missing");
			}
			if (password == null) {
				throw new ExceptionInInitializerError("Error: password is missing");
			}
			if (port == -1) {
				throw new ExceptionInInitializerError("Error: port is missing");
			}
			if (timeout == -1) {
				throw new ExceptionInInitializerError("Error: timeout is missing");
			}
		}

		private static String get(String key) {
			String value = null;
			if (LogSetting.LOG_REDIS) {
				logger.info("get Begin params: key= " + key);
			}
			if (key != null) {

				try (Jedis jedis = new Jedis(host, port, timeout)) {
					jedis.auth(password);
					jedis.connect();
					if (jedis.isConnected()) {
						value = jedis.get(key);
					} else {
						if (LogSetting.LOG_REDIS) {
							logger.info("Error: can't connect to Redis !!!!!");
						}
						throw new FailedException("can't connect to Redis");
					}
				}
			}
			if (LogSetting.LOG_REDIS) {
				logger.info("get End returns " + value);
			}
			return value;
		}

		public static void clearState(ByteAggregator byteAggregator) {
			String kTxid = byteAggregator.txidKey;
			String kFirstBlock = byteAggregator.firstblockKey;
			String kLastBlock = byteAggregator.lastblockKey;
			if (LogSetting.LOG_REDIS) {
				logger.info("clearState Begin");
				logger.info("clear keys " + kTxid + ", " + kFirstBlock + ", " + kLastBlock);
			}
			if (kTxid != null && kFirstBlock != null && kLastBlock != null) {
				try (Jedis jedis = new Jedis(host, port, timeout)) {
					jedis.auth(password);
					jedis.connect();
					if (jedis.isConnected()) {
						Transaction trans = jedis.multi();
						try {
							trans.del(kTxid);
							trans.del(kFirstBlock);
							trans.del(kLastBlock);
							trans.exec();
						} catch (Exception e) {
							trans.discard();
							throw new FailedException(e.getMessage());
						}
					} else {
						if (LogSetting.LOG_REDIS) {
							logger.info("Error: can't connect to Redis !!!!!");
						}
						throw new FailedException("can't connect to Redis");
					}
				}
			}
			if (LogSetting.LOG_REDIS) {
				logger.info("clearState End");
			}
		}

		static void setState(BlockState blockState) {
			if (LogSetting.LOG_REDIS) {
				logger.info("setState Begin");
			}
			String kTxid = blockState.byteAggregator.txidKey;
			String vTxid = String.valueOf(blockState.byteAggregator.txid);
			String kFirstBlock = blockState.byteAggregator.firstblockKey;
			String vFirstBlock = blockState.firstBlock.blobidAndBlockidStr;
			String kLastBlock = blockState.byteAggregator.lastblockKey;
			String vLastBlock = blockState.currentBlock.blobidAndBlockidStr;
			if (LogSetting.LOG_REDIS) {
				logger.info(blockState.partitionTxidLogStr + "set(" + kTxid + ") to" + vTxid);
				logger.info(blockState.partitionTxidLogStr + "set(" + kFirstBlock + ") to" + vFirstBlock);
				logger.info(blockState.partitionTxidLogStr + "set(" + kLastBlock + ") to" + vLastBlock);
			}
			try (Jedis jedis = new Jedis(host, port, timeout)) {
				jedis.auth(password);
				jedis.connect();
				if (jedis.isConnected()) {
					Transaction trans = jedis.multi();
					try {
						trans.set(kTxid, vTxid);
						trans.set(kFirstBlock, vFirstBlock);
						trans.set(kLastBlock, vLastBlock);
						trans.exec();
					} catch (Exception e) {
						trans.discard();
						throw new FailedException(e.getMessage());
					}
				} else {
					if (LogSetting.LOG_REDIS) {
						logger.info("Error: can't connect to Redis !!!!!");
					}
					throw new FailedException("can't connect to Redis");
				}
			}
			if (LogSetting.LOG_REDIS) {
				logger.info("setList End");
			}
		}
	}
}
