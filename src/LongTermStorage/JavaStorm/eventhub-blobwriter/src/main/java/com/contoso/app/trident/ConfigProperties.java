// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
package com.contoso.app.trident;

import java.io.IOException;
import java.util.Properties;

public final class ConfigProperties {
	private static final Properties properties = new Properties();

	static {
		try {
			properties.load(ConfigProperties.class.getClassLoader().getResourceAsStream("Config.properties"));
		} catch (IOException e) {
			throw new ExceptionInInitializerError(e);
		}
	}

	public static String getProperty(String key) {
		return properties.getProperty(key);
	}
}
