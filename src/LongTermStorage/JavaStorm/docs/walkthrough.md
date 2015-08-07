## Create Java Topology Project eventhub-blobwriter from Scratch

### Install Java Dependencies
Follow the section **Install Java Dependencies** in [Getting Started](docs/readme.md) to install the dependencies to your local Maven store.

### Clone the source code for the Reference Implementation
Clone this repository so you can copy the content of the java files.


### Scaffold the Storm topology project
Use the following Maven command to create the scaffolding for the Trident topology project.

```
mvn archetype:generate -DgroupId=com.contoso.app.trident -DartifactId=eventhub-blobwriter -DarchetypeArtifactId=maven-archetype-quickstart -DinteractiveMode=false
```

### Add dependencies in pom.xml
Using a text editor, open the pom.xml file, and add the following to the `<dependencies>` section. You can add them at the end of the section, after the dependency for junit.

``` xml
<dependency>
	<groupId>org.apache.storm</groupId>
	<artifactId>storm-core</artifactId>
	<version>0.9.1-incubating</version>
	<!-- keep storm out of the jar-with-dependencies -->
	<scope>${storm.scope}</scope>
</dependency>
<dependency>
	<groupId>com.google.guava</groupId>
	<artifactId>guava</artifactId>
	<version>13.0.1</version>
</dependency>
<dependency>
	<groupId>commons-collections</groupId>
	<artifactId>commons-collections</artifactId>
	<version>3.2.1</version>
</dependency>
<dependency>
	<groupId>org.slf4j</groupId>
	<artifactId>slf4j-api</artifactId>
	<version>1.7.7</version>
	<!-- keep out of the jar-with-dependencies -->
	<scope>provided</scope>
</dependency>
<dependency>
	<groupId>com.microsoft.eventhubs</groupId>
	<artifactId>eventhubs-storm-spout</artifactId>
	<version>0.9</version>
</dependency>
<dependency>
	<groupId>com.google.code.gson</groupId>
	<artifactId>gson</artifactId>
	<version>2.3</version>
</dependency>
<dependency>
	<groupId>redis.clients</groupId>
	<artifactId>jedis</artifactId>
	<version>2.6.2</version>
</dependency>

<dependency>
	<groupId>com.microsoft.azure</groupId>
	<artifactId>azure-storage</artifactId>
	<version>1.3.1</version>
</dependency>

<dependency>
	<groupId>com.microsoft.azure</groupId>
	<artifactId>azure-management</artifactId>
	<version>0.6.0</version>
</dependency>
<dependency>
	<groupId>com.microsoft.azure</groupId>
	<artifactId>azure-management</artifactId>
	<version>0.6.0</version>
</dependency>
<dependency>
	<groupId>com.microsoft.azure</groupId>
	<artifactId>azure-management-compute</artifactId>
	<version>0.6.0</version>
</dependency>
<dependency>
	<groupId>com.microsoft.azure</groupId>
	<artifactId>azure-management-network</artifactId>
	<version>0.6.0</version>
</dependency>
<dependency>
	<groupId>com.microsoft.azure</groupId>
	<artifactId>azure-management-sql</artifactId>
	<version>0.6.0</version>
</dependency>
<dependency>
	<groupId>com.microsoft.azure</groupId>
	<artifactId>azure-management-storage</artifactId>
	<version>0.6.0</version>
</dependency>
<dependency>
	<groupId>com.microsoft.azure</groupId>
	<artifactId>azure-management-websites</artifactId>
	<version>0.6.0</version>
</dependency>
<dependency>
	<groupId>com.microsoft.azure</groupId>
	<artifactId>azure-media</artifactId>
	<version>0.6.0</version>
</dependency>
<dependency>
	<groupId>com.microsoft.azure</groupId>
	<artifactId>azure-servicebus</artifactId>
	<version>0.6.0</version>
</dependency>
<dependency>
	<groupId>com.microsoft.azure</groupId>
	<artifactId>azure-serviceruntime</artifactId>
	<version>0.6.0</version>
</dependency>
<dependency>
	<groupId>com.microsoft.windowsazure.storage</groupId>
	<artifactId>microsoft-windowsazure-storage-sdk</artifactId>
	<version>0.6.0</version>
</dependency>
```

Note: Some dependencies are marked with a scope of *provided* to indicate that these dependencies should be downloaded from the Maven repository and used to build and test the application locally, but that they will also be available in your runtime environment and do not need to be compiled and included in the JAR created by this project.

### Add plugins in pom.xml
At the end of the pom.xml file, right before the `</project>` entry, add the following.

``` xml
<build>
	<plugins>
		<plugin>
			<groupId>org.apache.maven.plugins</groupId>
			<artifactId>maven-compiler-plugin</artifactId>
			<version>3.2</version>
			<configuration>
				<source>1.7</source>
				<target>1.7</target>
			</configuration>
		</plugin>
		<plugin>
			<groupId>org.apache.maven.plugins</groupId>
			<artifactId>maven-shade-plugin</artifactId>
			<version>2.3</version>
			<configuration>
				<createDependencyReducedPom>true</createDependencyReducedPom>
				<transformers>
					<transformer
						implementation="org.apache.maven.plugins.shade.resource.ApacheLicenseResourceTransformer">
					</transformer>
				</transformers>
			</configuration>
			<executions>
				<execution>
					<phase>package</phase>
					<goals>
						<goal>shade</goal>
					</goals>
					<configuration>
						<transformers>
							<transformer
								implementation="org.apache.maven.plugins.shade.resource.ServicesResourceTransformer" />
							<transformer
								implementation="org.apache.maven.plugins.shade.resource.ManifestResourceTransformer">
								<mainClass></mainClass>
							</transformer>
						</transformers>
					</configuration>
				</execution>
			</executions>
		</plugin>
		<plugin>
			<groupId>org.codehaus.mojo</groupId>
			<artifactId>exec-maven-plugin</artifactId>
			<version>1.2.1</version>
			<executions>
				<execution>
					<goals>
						<goal>exec</goal>
					</goals>
				</execution>
			</executions>
			<configuration>
				<executable>java</executable>
				<includeProjectDependencies>true</includeProjectDependencies>
				<includePluginDependencies>false</includePluginDependencies>
				<classpathScope>compile</classpathScope>
				<mainClass>${storm.topology}</mainClass>
			</configuration>
		</plugin>
	</plugins>
	<resources>
		<resource>
			<directory>${basedir}/conf</directory>
			<filtering>false</filtering>
			<includes>
				<include>Config.properties</include>
			</includes>
		</resource>
	</resources>
</build>
```

This tells Maven to do the following when building the project:
- Include the /conf/Config.properties resource file. This file will be created later, it will contain configuration information for connecting to Azure Event Hub.
- Use the maven-compiler-plugin to compile the application.
- Use the maven-shade-plugin to build an uber jar or fat jar, which contains this project and any required dependencies.
- Use the exec-maven-plugin, which allows you to run the application locally without a Hadoop cluster.

### Add configuration file
eventhubs-storm-spout reads configuration information from a Config.properties file. This tells it what Event Hub to connect to. While you can specify a configuration file when starting the topology on a cluster, including one in the project gives you a known default configuration.
- In the eventhub-blobwriter directory, create a new directory named conf. This will be a sister directory of src.
- In the conf directory, create file Config.properties - contains settings for event hub
- Copy the cloned content to Config.properties.template file.
- You should modify the value according to your settings.

### Add Java classes

#### Add BlobWriter class support uploading data to azure blob

- Create a new file BlobWriter.java in directory \eventhub-blobwriter\src\main\java\com\contoso\app\trident\
- Copy the content from the cloned file to the above file.

#### Add BlobWriterTopology class

- Create a new file BlobWriterTopology.java in directory \eventhub-blobwriter\src\main\java\com\contoso\app\trident\
- Copy the content from the cloned file to the above file.

#### Add Block class

- Create a new file Block.java in directory \eventhub-blobwriter\src\main\java\com\contoso\app\trident\
- Copy the content from the cloned file to the above file.

#### Add BlockState class

- Create a new file BlockState.java in directory \eventhub-blobwriter\src\main\java\com\contoso\app\trident\
- Copy the content from the cloned file to the above file.

#### Add BlockStateStore class

- Create a new file BlockStateStore.java in directory \eventhub-blobwriter\src\main\java\com\contoso\app\trident\
- Copy the content from the cloned file to the above file.

#### Add ByteAggregator class to perform partitionAggregate operation

- Create a new file ByteAggregator.java in directory \eventhub-blobwriter\src\main\java\com\contoso\app\trident\
- Copy the content from the cloned file to the above file.

#### Add ConfigProperties class

- Create a new file ConfigProperties.java in directory \eventhub-blobwriter\src\main\java\com\contoso\app\trident\
- Copy the content from the cloned file to the above file.

#### Add LogSetting class

- Create a new file LogSetting.java in directory \eventhub-blobwriter\src\main\java\com\contoso\app\trident\
- Copy the content from the cloned file to the above file.

### Build the project
Use the following command to create a JAR package from your project.
- Start command prompt

```
cd eventhub-blobwriter
mvn package
```
