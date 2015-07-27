# Getting Started with the Reference Implementation
Store Event Hub Messages to Microsoft Azure Blob with Trident

[Microsoft patterns & practices][pnp]

## Prerequisites
- [An Azure subscription][azure]
- [Java JDK][java-dl] (We've used JDK7)
- [Maven][maven-dl]
- [Visual Studio][vs] with the [Microsoft Azure SDK for .NET][azure-dl]
- A [Git][git] client

## Configure Microsoft Azure

### Create the Azure Storage Account
- Create an Azure Storage account.
  *Refer to [Create a storage account][azure-storage] for instructions.*

### Create the Azure Redis Cache
- Create an Azure Redis Cache.  
  *Refer to the "Create a Redis cache on Azure" section of [How to use Azure Redis Cache with Java][azure-redis-java] for instructions.*

  - Be sure to enable the non-SSL endpoint, as described in that document.

### Configure Event Hub
- Create an Azure Service Bus Event Hub with partition count 10 and message retention of 1 days.  
  *Refer to the "Create an Event Hub" section of [Get started with Event Hubs][azure-eventhubs] for instructions.*

- Once the event hub has been created, use the Azure Management Portal to select the event hub instance.
  Select *Configure*, then create two new *shared access policies* using the following information.

NAME    | PERMISSIONS
----    | -----------
devices | Send
storm   | Listen

### Create the HDInsight Storm cluster
- Create an Azure HDInsight Storm cluster.  
  *Refer to the "Provision a Storm cluster on the Azure portal" section of [Getting started using Storm on HDInsight][azure-storm]
  for instructions.*


## Install Java Dependencies

Several of the dependencies in *eventhub-blobwriter* java storm project must be downloaded and built individually,
then installed into the local Maven repository on your development environment.

### Install Microsoft Azure SDK for Java
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

### Install Microsoft Azure Storage libraries for Java
- Clone [Microsoft Azure Storage libraries for Java][azure-storage-java]
- Use the following command to install the package into the local Maven store.
  This will allow you to easily add it as a reference in the Storm project in a later step.

```
mvn clean install -Dmaven.test.skip=true
```

### Install the Event Hub spout
In order to receive data from Event Hub, we will use the eventhubs-storm-spout.
- Use Remote Desktop to connect to your Storm cluster.  You can connect easily from the HDInsight section of the Azure Management Portal.
- Through the remote desktop session, copy the `eventhubs-storm-spout-0.9-jar-with-dependencies.jar` file,
  located at `%STORM_HOME%\examples\eventhubspout`, to a folder on your local machine.
- On your local machine, use the following command to install the package into the local Maven store.
  This will allow us to easily add it as a reference in the Storm project in a later step.

```
mvn install:install-file -Dfile=eventhubs-storm-spout-0.9-jar-with-dependencies.jar -DgroupId=com.microsoft.eventhubs -DartifactId=eventhubs-storm-spout -Dversion=0.9 -Dpackaging=jar
```

## Clone the source code of the Reference Implementation
If you haven't already, clone the [data-pipeline-storm][pnp-storm] project (this project).
Or, if you wish to commit changes, then fork this repository and clone your fork.

There are two projects in the `src` directory.

- **SendEvents:** A .NET console application written in C# that sends messages to an Azure Event Hub.

- **eventhub-blobwriter:** The Java implementation of Storm/Trident topology.  
  The document [Create Java Topology project eventhub-blobwriter from Scratch][walkthrough]
  walks you through the steps of how the above project was created.

## Open eventhub-blobwriter in an IDE (optional)

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

## Modify the configurations

### Modify Config.properties

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

### Modify Configuration for SendEvent

- Using the Windows file explorer, browse to the `data-pipeline-storm/src/SendEvents/SendEvents` folder.
- Copy the `App.config.template` file to a new file called `App.config`
- Start Visual Studio, and open the `SendEvents.sln` solution, under the `data-pipeline-storm/src/SendEvents` folder.
- Open the `App.config` file, and set the values according to your configuration settings:

``` xml
<add key="EventHubName" value="[your event hub name]"/>
<add key="EventHubNamespace" value="[your event hub namespace]"/>
<add key="DevicesSharedAccessPolicyName" value="devices"/>
<add key="DevicesSharedAccessPolicyKey" value="[shared access key for the 'devices' policy of your event hub]"/>
<add key="RootManageSharedAccessKey" value="[your service bus root management shared access key]"/>
```

## Run the topology

### Run the topology on development machine

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

### Run the topology in the HDInsight Storm Cluster
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
