##Introduction

The two primary concerns of this project are:

* Facilitating cold storage of data for later analytics.
That is, translating the _chatty_ stream of events into _chunky_ blobs.

* Demonstrate how to use OpaqueTridentEventHubSpout and
Apache Storm/Trident to store Microsoft Azure Eventhub messages to Microsoft Azure
Blob exactly-once.


## Getting Started

### Azure Prerequisites

:construction: **TBD: Deifine if we are going to have manual prerequisite steps here or we are providing an install script** 

 1. Create an Azure Storage Account
 2. Create Azure Redis Cache
 3. Configure Event Hub
 4. Create an HDInsight Storm Cluster
 
### Install Java Dependencies
Several of the dependencies in *eventhub-blobwriter* java storm project must be downloaded and built individually,
then installed into the local Maven repository on your development environment.

#### Install Microsoft Azure SDK for Java
- Clone [Microsoft Azure SDK for Java][azure-java]
- Use the following command to install the package into the local Maven store.
  This will allow you to easily add it as a reference in the Storm project in a later step.

```
mvn clean install -Dmaven.test.skip=true
```

> *If you are in a Powershell window, you will need to escape any arguments that start with a hyphen, using a backtick as follows:*
>
>     mvn clean install `-Dmaven.test.skip=true
>
> *This also applies to the other commands in this document.*

#### Install Microsoft Azure Storage libraries for Java
- Clone [Microsoft Azure Storage libraries for Java][azure-storage-java]
- Use the following command to install the package into the local Maven store.
  This will allow you to easily add it as a reference in the Storm project in a later step.

```
mvn clean install -Dmaven.test.skip=true
```

#### Install the Event Hub spout
In order to receive data from Event Hub, we will use the eventhubs-storm-spout.
- Use Remote Desktop to connect to your Storm cluster.  You can connect easily from the HDInsight section of the Azure Management Portal.
- Through the remote desktop session, copy the `eventhubs-storm-spout-0.9-jar-with-dependencies.jar` file,
  located at `%STORM_HOME%\examples\eventhubspout`, to a folder on your local machine.
- On your local machine, use the following command to install the package into the local Maven store.
  This will allow us to easily add it as a reference in the Storm project in a later step.

```
mvn install:install-file -Dfile=eventhubs-storm-spout-0.9-jar-with-dependencies.jar -DgroupId=com.microsoft.eventhubs -DartifactId=eventhubs-storm-spout -Dversion=0.9 -Dpackaging=jar
```
### Open eventhub-blobwriter in an IDE (optional)

You can choose to import the project into an IDE.  We've tested under Eclipse and IntelliJ.

#### [Eclipse IDE][eclipse-dl]
- Start Eclipse IDE
- Select *File* - *Switch Workspace* - *Other...*
- Choose the `data-pipeline-storm/src` folder.  Click *OK*. Eclipse will restart.
- Select *File* - *Import...*.  Then select *Maven* - *Existing Maven Projects*.  Click *Next*.
- Choose `data-pipeline-storm/src` for the root directory.
- Select `/eventhub-blobwriter/pom.xml` from the projects list,  Click *Finish*.
- You should be able to find the project's `.java` files in the Package Explorer, under the `src/main/java` folder,
  in the `com.contoso.app.trident` package.

#### [IntelliJ IDEA][intellij-dl]
- Start IntelliJ IDEA
- Click *Import Project*
- Choose the `data-pipeline-storm/src/eventhub-blobwriter` folder.  Click *OK*.
- Select *Import  project from external model* and choose *Maven*.  Click *Next*.
- Adjust the project import settings if desired.  (The defaults will work fine.)  Click *Next*.
- The eventhub-blobwriter Maven project should be selected by default. Click *Next*.
- Choose your JDK version.  (We've tested under 1.7.) Click *Next*.
- Click Finish.
- You should be able to find the project's `.java` files in the Project window, under the `src/main/java` folder,
  in the `com.contoso.app.trident` package.

> *IntelliJ may prompt you with "Unregistered Vcs root detected".  
> If you plan to make changes and are  working against your own fork, then click "Add root"
> to enable Git integration.  Otherwise, click "Ignore".*

#### Modify the configurations

##### Modify Config.properties

- Open the `/eventhub-blobwriter/conf` folder.
- Copy the `Config.properties.template` file to a new file called `Config.properties`.
- Open the `Config.properties` file, and set the following values according to your configuration settings:

```
eventhubspout.password = [shared access key for the "storm" policy of your event hub]
eventhubspout.namespace = [your service bus namespace]
eventhubspout.entitypath = [your event hub name]

storage.blob.account.name = [your storage account name]
storage.blob.account.key = [your storage account key]

redis.host = [your redis host name].redis.cache.windows.net
redis.password = [your redis access key]
```

If desired, you can enable logging for any of the items listed in the `LogSettings` section.
All logging is turned off by default.  Change a setting to `true` to turn on logging for that item.

```
LOG_BATCH = false
LOG_MESSAGE = false
LOG_MESSAGEROLLOVER = false
LOG_BLOCK = false
LOG_BLOBWRITER = false
LOG_BLOBWRITERDATA = false
LOG_REDIS = false
```

##### Modify Simulator configuration
:construction: **TODO: provide guidance on how to configure the simulator to send events to EventHub (this can be skiped in case we provide scripts)** 

### Run the topology

#### Run the topology on development machine

To run on your development machine, use the following steps.
- Start the SendEvent .NET application to begin sending events, so that the topology has something to read from Event Hub.
- Start the topology locally. This will read messages from Event Hub and upload them to azure blob storage.

  **Option 1 - Run from the command line**
  - You can also start the topology by running the following command line from the `/src/eventhub-blobwriter` folder:

        mvn compile exec:java -Dstorm.topology=com.contoso.app.trident.BlobWriterTopology -Dstorm.scope=provided

  - You can stop the topology by pressing Ctrl-C.

  **Option 2 - Run from Eclipse**  
  - Open the `BlobWriterTopology.java` file.
  - Run the topology.  (Press Ctrl-F11.)
  - Top stop the topology, press the red *Terminate* icon in the Console window.

  **Option 3 - Run from IntelliJ**
  - Open the `BlobWriterTopology.java` file.
  - Click `Run...` from the `Run` menu.
    - It will ask you to create the run configuration.  You can simply select the default "BlobWriterTopology.java" configuration.
    - The topology should start in the Run window.
  - Top stop the topology, press the red *Terminate* icon in the Run window.

- Verify that the message are uploaded to azure blob.
  - Download, install, and start [Azure Storage Explorer][azure-storage-explorer].
  - Click **Add Account** to add your storage account.
  - Click **refresh** button and then click on the container for the uploaded blobs.
  - The container will be named `eventhubblobwriter`, followed by the time that the topology was started.
  - Note: You will get a new container every time you restart the topology.

#### Run the topology in the HDInsight Storm Cluster
In your development environment, use the following steps to run the topology on your HDInsight Storm cluster.

- Start the SendEvent .NET application to begin sending events, so that the topology has something to read from Event Hub.
- Change to the `/src/eventhub-blobwriter` folder if you aren't already there.
- Use the following command to ask Maven to create a JAR package from your project.

        mvn clean package -Dstorm.scope=provided

  This will create a file named `eventhub-blobwriter-1.0-SNAPSHOT.jar` in the `target` subdirectory of your project.

- Deploy the jar file to your HDInsight Storm cluster through the Storm Dashboard in the Azure Management Portal.
  This is also described in the document: [Deploy and manage Apache Storm topologies on HDInsight][azure-storm-deploy].
  - Sign in to the Storm Dashboard through the Azure Management Portal.
  - In the "Jar File" dropdown, select "Upload new Jar - Browse", then choose the jar file you created with Maven in the previous step.
  - In the "Class Name" field, enter `com.contoso.app.trident.BlobWriterTopology`
  - In the "Additional Parameters" field, enter any name you would like to use to identify the topology.
    For example, `MyBlobWriterTopology`.  This will show up in the Storm portal and a few other places, and will help you to
    distinguish one topology from the next.
  - Click Submit, and wait for your topology to be submitted.

- Verify that the message are uploaded to azure blob.
  - Download, install, and start [Azure Storage Explorer][azure-storage-explorer].
  - Click **Add Account** to add your storage account.
  - Click **refresh** button and then click on the container for the uploaded blobs.
  - The container will be named `eventhubblobwriter`, followed by the time that the topology was started.
  - Note: You will get a new container every time you restart the topology.

- You can control the topology by using the buttons in the Storm Dashboard.
  - Select the "Storm UI" tab.
  - Click the link for your topology under the "Topology Summary" section.
  - Use the buttons under "Topology actions"
    - `Deactivate` will pause the topology.
    - `Activate` will restart the toplogy.
    - `Kill` will terminate the toplogy completely.
    - `Rebalance` should be used after adjusting the number of nodes in the cluster.
      See [Understanding the Parallelism of a Storm Topology][storm-parallel] for more information.

[azure]: http://azure.microsoft.com/
[azure-dl]: http://azure.microsoft.com/en-us/downloads/
[azure-eventhubs]: http://azure.microsoft.com/en-us/documentation/articles/service-bus-event-hubs-csharp-ephcs-getstarted/#create-an-event-hub
[azure-java]: https://github.com/Azure/azure-sdk-for-java
[azure-redis-java]: http://azure.microsoft.com/en-us/documentation/articles/cache-java-get-started/
[azure-storm]: http://azure.microsoft.com/en-us/documentation/articles/hdinsight-storm-getting-started/#provision-a-storm-cluster-on-the-azure-portal
[azure-storm-deploy]: http://azure.microsoft.com/en-us/documentation/articles/hdinsight-storm-deploy-monitor-topology/
[azure-storage]: http://azure.microsoft.com/en-us/documentation/articles/storage-create-storage-account/
[azure-storage-explorer]: https://azurestorageexplorer.codeplex.com/
[azure-storage-java]: https://github.com/Azure/azure-storage-java
[eclipse-dl]: https://www.eclipse.org/downloads/
[git]: http://git-scm.com/
[intellij-dl]: https://www.jetbrains.com/idea/download/
[java-dl]: http://www.oracle.com/technetwork/java/javase/downloads/index.html
[maven-dl]: http://maven.apache.org/download.cgi
[pnp]: http://aka.ms/mspnp
[pnp-storm]: https://github.com/mspnp/data-pipeline-storm
[storm-parallel]: http://storm.apache.org/documentation/Understanding-the-parallelism-of-a-Storm-topology.html
[vs]: http://www.visualstudio.com/en-us/products/visual-studio-community-vs
[walkthrough]: /docs/step-by-step-walkthrough.md

## Next Steps

* [Architecture Overview](docs/architecture-overview.md)
* [Create Java Topology Project eventhub-blobwriter from Scratch](docs/walkthrough.md)
* [Design Considerations and Technical How-To](docs/design-considerations.md)