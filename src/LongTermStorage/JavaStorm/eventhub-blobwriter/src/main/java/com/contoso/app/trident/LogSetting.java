// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
package com.contoso.app.trident;

public final class LogSetting {
	public static final boolean LOG_BATCH = Boolean.parseBoolean(ConfigProperties.getProperty("LOG_BATCH"));
	public static final boolean LOG_BLOBWRITER = Boolean.parseBoolean(ConfigProperties.getProperty("LOG_BLOBWRITER"));
	public static final boolean LOG_BLOBWRITERDATA = Boolean.parseBoolean(ConfigProperties.getProperty("LOG_BLOBWRITERDATA"));
	public static final boolean LOG_MESSAGE = Boolean.parseBoolean(ConfigProperties.getProperty("LOG_MESSAGE"));
	public static final boolean LOG_BLOCK = Boolean.parseBoolean(ConfigProperties.getProperty("LOG_BLOCK"));
	public static final boolean LOG_MESSAGEROLLOVER = Boolean.parseBoolean(ConfigProperties.getProperty("LOG_MESSAGEROLLOVER"));
	public static final boolean LOG_REDIS = Boolean.parseBoolean(ConfigProperties.getProperty("LOG_REDIS"));
}
