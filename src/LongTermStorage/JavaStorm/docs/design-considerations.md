## Design Considerations and Technical How-To

### Batching

If we don’t need batching, we can use regular storm spouts and bots. If we need batching, we should consider using Trident since it is batch based. since we need to group messages in to azure blocks, batching makes sense.

### Dropped messages

If dropped messages is allowed, we can use non-transactional spout, which will not replay when messages processing fails. In that case, some messages will not be stored in Azure blob. If dropped messages is not allowed, we need **at-least-once** scenario of message processing.  We can considering using a transactional or opaque transactional spout, which can replay the messages if messages processing fails.

### Duplicated Messages

If we can tolerate duplicated messages to be stored in azure blobs, we can achieve at-least-once scenario with batching or without batching.

- at-least-once scenario without batching: We can use Storm’s reliability capabilities. We must tell Storm when new edges in a tuple tree are being created and tell Storm whenever we have finished processing an individual tuple. These can be done using the OutputCollector object that bolts use to emit tuples. Anchoring is done in the emit method, and we declare that we are finished with a tuple using the ack method.

- at-least-once scenario with batching: We can use transactional or opaque transactional trident spout and don’t need any additional logic to handle the replay. If a transaction fails, the Trident will replay the batch. When processing the replay, some message will be stored in azure blobs again and result in duplication or multiple entry for the same message.

### De-duplication

If we use Trident transactional or opaque transactional spout, we can be sure that the state updates ordered among batches (i.e. the state updates for batch 4 won’t be applied until the state updates for batch 3 have succeeded). If the state updates for batch 3 failed, batch 3 will be replayed. However, the replay only guarantees at-least-once scenario. It up to us to implement the de-duplication logic during a replay. To implement that, we need to first figure out whether we are in the replay or not when processing a batch.

### How do we know if we are inside a replay?

If we are using Trident, we can get the **TransactionId** from **TransactionAttempt** object, which is passed in as an Object in the **Aggregator** init method.  During a replay, the value of TransactionId will be the same as the that of the previous batch.  If we save the previous TransactionId, all we need to do if to compare the current value with the saved value. If the value is the same, we are inside a replay. If there are different, we are inside a fresh new batch.

## Strorm vs Trident

### Should we use a Storm topology or Trident topology?

A Storm structure is called topology.  A topology consists of stream of data, spouts that produce the data, and bots that process the data. Storm topologies run forever, until explicitly killed.

Trident is a high-level abstraction on top of Storm. Trident process messages in batches and it consists of joins, aggregations, grouping, functions, and filters. Trident support exactly-once semantics.

Sincne the core business scenario for the reference implementation is to aggregate individual messages into an Azure block, the batching feature provided by Trident makes it a good choice. We can simply aggregate a batch into a block if the batch can fits. If a batch is bigger than the max size of a block, we can split the batch up in to several blocks. The only limitation is that when the batch is smaller than a block, in which case, the block will not be filled up with its max size.

### What are built-in stream groupings in Storm and what kind of grouping are suitable for our scenario?

Storm has seven built-in stream groupings:

1. Shuffle grouping

2. Fields grouping

3. All grouping

4. Global grouping

5. None grouping

6. Direct grouping

7. Local or shuffle grouping

We need to have all messaged in an event hub partition stored in the same Azure blobs. So if we decide to use Storm, it's natural that we want use Fields grouping with partitionId. That means that the spout need to emit the partitionID.

However, if we use Trident, the core data model in Trident is the "Stream", processed as a series of batches. A stream is partitioned among the nodes in the cluster, and operations applied to a stream are applied in parallel across each partition. No explicit grouping is needed for partitionID.

### What are Trident the operations and which are suitable for our scenario?

There are five kinds of operations in Trident:

1. Partition-local Operations

2. Repartitioning operations

3. Aggregation operations

4. Operations on grouped streams

5. Merges and joins

We want to messages in each partition to be aggregated and stored in its corresponding Azure blobs. So we should pick the first one: Operations that apply locally to each partition and cause no network transfer.

### What are Partition-local Operations in Trident?

Partition-local operations involve no network transfer and are applied to each batch partition independently. They include **Functions**, **Filters**, and **partitionAggregate (Aggregator)**.
To support the exactly-once semantics, we need to know whether we are in a replay, whether all the tuples in a batch are processed. We decide to use the most general interface: **Aggregator""

### What is an Trident Aggregator and why it fits our needs?
The most general interface for performing aggregations is Aggregator, which looks like this:

``` java
public interface Aggregator<T> extends Operation {
  T init(Object batchId, TridentCollector collector);
  void aggregate(T state, TridentTuple tuple, TridentCollector collector);
  void complete(T state, TridentCollector collector);
}
```
Aggregators can emit any number of tuples with any number of fields. They can emit tuples at any point during execution. Aggregators execute in the following way:

1.  The init method is called before processing the batch.

2.  The aggregate method is called for each input tuple in the batch partition.

3.  The complete method is called when all tuples for the batch partition have been processed by aggregate.

## Implementation Details

### What’s the implementation of ByteAggregator?

Let's call the aggregator in  the reference implementation ByteAggregator, we can extend the BaseAggregator instead of directly implemenat the Aggregator interface:

``` java
public class ByteAggregator extends BaseAggregator<T>
```

The following code shows a stripped-down implementation of ByteAggregator:

``` java
public class ByteAggregator extends BaseAggregator<BlockList> {
  private long txid;
  private int partitionIndex;

  public void prepare(@SuppressWarnings("rawtypes") Map conf,TridentOperationContext context) {
    this.partitionIndex = context.getPartitionIndex();
  }

  public BlockList init(Object batchId, TridentCollector collector) {
    this.txid = ((TransactionAttempt) batchId).getTransactionId();
    Blocklist blockList = new Blocklist(this.partitionIndex, this.txid,this.properties);
    return blockList;
  }

  public void aggregate(BlockList blockList, TridentTuple tuple,TridentCollector collector) {
    String msg = tuple.getString(0) + "\r\n";
    if (blockList.currentBlock.willMessageFitCurrentBlock(msg)) {
        blockList.currentBlock.addData(msg);
    } else {
        blockList.currentBlock.upload();
    }
  }

  public void complete(BlockList blockList, TridentCollector collector) {
    blockList.persist();//persist blockList ids, txid, partitionid to support roll back
  }
}
```

Here are the key point of ByteAggregate class:

1. paritionIndex is retrieved in the the prepare method, which is defined in the operation interface. prepare is called once for each partition when the topology starts.

2. txid is retrieved in the init method, which get called by the Trident before processing each batch.

3. tuple string is added into to the block data in aggregate method. When a block is filled up, it is uploaded to Azure storage.

4. the blockList is persisted in the complete method, which stores partitionid, transactionid, blockname, and blockids to Redis cache. In case next replay, those blocks that are uploaded to Azure storage will be over written to remove the duplication. If next batch is not a replay, the previous uploaded blocks will be permanent.

## Techinical How-To

### How to create a Storm or Trident project?

Run

```
mvn archetype:generate -DgroupId=com.mycompany.app -DartifactId=my-app -DarchetypeArtifactId=maven-archetype-quickstart -DinteractiveMode=false
```

Edit pom.xml  file and add the Storm dependency:

```
<dependency> <groupId>org.apache.storm</groupId> <artifactId>storm-core</artifactId> <version>0.9.1-incubating</version> </dependency>
```

Run

```
mvn clean package
```

For Strom topology, add java class for spout, bolt, and topology. In case of a Trident topology, add java class for operations. Add class to build the topology, and press F11 to test locally (in Eclipse) or deploy jar file to a storm headnote.

### How to send event to Event Hub?

:construction: **TODO: include a reference to the Simulator** 

### Is there any existing storm spout for event hub?

Yes. [Analyzing sensor data with Storm and HBase in HDInsight (Hadoop)](http://azure.microsoft.com/en-us/documentation/articles/hdinsight-storm-sensor-data-analysis/) has the sample code for using storm spout for event hub.

### How to use the eventhub spout included in the HDI Storm in your java program?
Here are the steps:

- Copy the jar file C:\apps\dist\storm-0.9.1.2.1.6.0-2103\examples\eventhubspout\eventhubs-storm-spout-0.9-jar-with-dependencies.jar from the HDI storm head node to your development PC.
-	Add that jar file to the local maven repo:
mvn install:install-file -Dfile=eventhubs-storm-spout-0.9-jar-with-dependencies.jar -DgroupId=com.microsoft.eventhubs -DartifactId=eventhubs-storm-spout -Dversion=0.9 -Dpackaging=jar
-	Add the jar to the pom:

```
<dependency>
      <groupId>com.microsoft.eventhubs</groupId>
      <artifactId>eventhubs-storm-spout</artifactId>
      <version>0.9</version>
</dependency>
```

- Enable distribution of the jar file by adding the following to the pom after ```<build><plugins>```:


```
<plugin>
<groupId>org.apache.maven.plugins</groupId>
<artifactId>maven-shade-plugin</artifactId>
<version>2.3</version>

<configuration>
<createDependencyReducedPom>true</createDependencyReducedPom>
<transformers>
<transformer implementation="org.apache.maven.plugins.shade.resource.ApacheLicenseResourceTransformer">
</transformer>
</transformers>
</configuration>
<executions><execution>
<phase>package</phase>
<goals><goal>shade</goal></goals>
<configuration>
<transformers>
<transformer implementation="org.apache.maven.plugins.shade.resource.ServicesResourceTransformer" />
<transformer implementation="org.apache.maven.plugins.shade.resource.ManifestResourceTransformer">
<mainClass></mainClass>
</transformer>
</transformers>
</configuration>
</execution></executions>
</plugin>
```

### Is there an existing implementation of trident transactional spout for event hub?
The storm cluster headnode C:\apps\dist\storm-0.9.1.2.1.6.0-2103\examples\eventhubspout\eventhubs-storm-spout-0.9-jar-with-dependencies.jar consists of two trident spout
-	OpaqueTridentEventHubSpout
-	TransactionalTridentEventHubSpout

### How many instances of spout should I have?
The number of spout should be equal to the number of event hub partitions.

```
EventHubSpoutConfig spoutConfig = new EventHubSpoutConfig(…,eventHubPartitionCount,…);
OpaqueTridentEventHubSpout spout = new OpaqueTridentEventHubSpout(spoutConfig);
```

### How to configure the topology so that each partitioned aggregate will read from its corresponding spout?

The number of workers (partitioned aggregate) should be equal to the Event Hub Partition Count.

``` java
int numWorkers = eventHubPartitionCount;
EventHubSpoutConfig spoutConfig = new EventHubSpoutConfig(…,eventHubPartitionCount,…);
OpaqueTridentEventHubSpout spout = new OpaqueTridentEventHubSpout(spoutConfig);
Stream inputStream = tridentTopology.newStream("message", spout);
inputStream.parallelismHint(numWorkers).partitionAggregate(new Fields("message"), new ByteAggregator(), new Fields("blobname"));
```

Currently we automatically assign partitions to tasks depending on task ID. E.g. task 0 receive from partition 0, task 1 receive from partition 1 etc. For trident this is the only supported assignment scheme.

### The event hub has 8 partitions. Can I configure my trident topology to have 8 tasks (instances) of the spout?
Yes, actually you can only set the number of tasks to a value between 1 to 8 if your event hub have 8 partitions. We recommend set the number of tasks to the number of partitions.

### Can I configure/modify the spout to emit the partition id with each tuple?
No.  You cannot at this moment.

## Programming Transaction

### How to get transaction id in trident code?
You can get the transaction ID in the init method of Aggregator.

``` java
public T init(Object batchId, TridentCollector collector) {
  if (batchId instanceof TransactionAttempt) {
    txid = ((TransactionAttempt) batchId).getTransactionId();
  }
  return new T();
}
```

### How to get partition id in trident code?
You can get the partition id in the prepare method of your operation.

``` java
@override
public void prepare(Map conf,  TridentOperationContext context)
{
    Super.prepare(conf, context);
    this.partitionIndex = context.getPartitionIndex();
}
```

### How to cause a replay in trident?

Throw FailedException will cause a replay.

``` java
catch (Exception e) {
    throw new FailedException(e.getMessage());
}
```

## Write and append to azure blob

### How to Convert azure blob url to wasb

Blob url:  http:// mystorage.blob.core.windows.net/mycontainer/folder/file.txt

WASB:    wasb://mycontainer@mystorage.blob.core.windows.net/folder/file.txt

### How to write to Azure blob in java?
add pom dependency:

```
<groupId>org.apache.hadoop</groupId>
<artifactId>hadoop-client</artifactId>
```

Java Code:

``` java
  Configuration conf = new Configuration();
  FileSystem hdfs = FileSystem.get(conf);
  Path path = new Path("wasb://container@storage.blob.core.windows.ent/a/b.txt");
  FSDataOutputStream stream = hdfs.create(path);
  byte[] bytes = "test data".getBytes();
  stream.write(bytes);
  stream.close();
```


### Can I use hdfs.append(path) to append to azure blob in a java as shown in the following code?


``` java
  Configuration conf = new Configuration();
  FileSystem hdfs = FileSystem.get(conf);
  Path path = new Path("wasb://container@storage.blob.core.windows.ent/a/b.txt");
                FSDataOutputStream stream = hdfs.append(path);
  byte[] bytes = "test data".getBytes();
  stream.write(bytes);
  stream.close();
```

The above code will not work for the Azure blob. It only works if the path point to HDFS file system

### Any existing bolt that can write/append to azure blob with a given size?

There is an open source bolt storm-hdfs that can write/append to hdfs:
-	Git repo: https://github.com/ptgoetz/storm-hdfs
-	Also available at head node

The following example will write pipe("|")-delimited files to the HDFS path hdfs://localhost:54310/foo. After every 1,000 tuples it will sync filesystem, making that data visible to other HDFS clients. It will rotate files when they reach 5 megabytes in size.

``` java
// use "|" instead of "," for field delimiter
RecordFormat format = new DelimitedRecordFormat().withFieldDelimiter("|");
// sync the filesystem after every 1k tuples
SyncPolicy syncPolicy = new CountSyncPolicy(1000);
// rotate files when they reach 5MB
FileRotationPolicy rotationPolicy = new FileSizeRotationPolicy(5.0f, Units.MB);
FileNameFormat fileNameFormat = new DefaultFileNameFormat().withPath("/foo/");

HdfsBolt bolt = new HdfsBolt()
        .withFsUrl("hdfs://headnodehost:9000")
        .withFileNameFormat(fileNameFormat)
        .withRecordFormat(format)
        .withRotationPolicy(rotationPolicy)
        .withRotationPolicy(syncPolicy)
```

However, the sample is for hdfs. For wasb, .withFsUrl("wasb://hanzstorm2@hanzstorage1.blob.core.windows.net/aaastorm2")
It throws Exception: java.lang.RuntimeException: Error preparing HdfsBolt: No FileSystem for scheme: wasb at org.apache.storm.hdfs.bolt.AbstractHdfsBolt.prepare(AbstractHdfsBolt.java:96) at backtype.storm.daemon.execu

### How do I copy files in hdfs://headnodehost:9000 to local file system in hadoop?

```
Hdfs dfs –fs hdfs://headnodehost:9000 –ls /
Hdfs dfs –fs hdfs://headnodehost:9000 –copyToLocal /foo/*.txt c:/temp
```

### How do I to provision storm cluster with append to hdfs enabled?
You cannot use the azure portal to provision an hdinsight cluster with dfs.support.append to true. But you can use powershell to do that:

```
New-AzureHDInsightClusterConfig -ClusterSizeInNodes 4 -ClusterType "Storm”|Add-AzureHDInsightConfigValues -Hdfs @{"dfs.support.append"="true"}
```

Note: the following does not work in HDInsight:

```
hadoop jar c:\hanz.jar storm.blueprints.chapter1.v1.WriteToBlob "-Ddfs.support.append=true" …. Note: this does not work in hdInsight
```

Sample PowerShell script :

```
# Ensure to install and Configure Windows Azure PowerShell from:
# http://azure.microsoft.com/en-us/documentation/articles/install-configure-powershell/
Add-AzureAccount
$ClusterName = "myclustername"
$DefaultContainerName = $ClusterName
$ClusterLocation = "West US"
$NumClusterNodes = 4
$ClusterVersion = "3.1"
$HDInsightUserName = "admin"
$HDInsightPwd = "MyPassword"
$ClusterType = "Storm"
$DefaultStorageAccountFqdn = "hanzstorage.blob.core.windows.net"
$Key1 = Get-AzureStorageKey "hanzstorage" | %{ $_.Primary }
$HdInsightPwd = ConvertTo-SecureString $HDInsightPwd -AsPlainText -Force
$HdInsightCreds = New-Object System.Management.Automation.PSCredential ($HDInsightUserName, $HdInsightPwd)

$HdfsConfigValues = @{ "dfs.support.append"="true" } # hdfs-site.xml configuration
$Config = New-AzureHDInsightClusterConfig -ClusterSizeInNodes $NumClusterNodes -ClusterType $ClusterType |
    Set-AzureHDInsightDefaultStorage -StorageAccountName $DefaultStorageAccountFqdn -StorageAccountKey $key1 -StorageContainerName $DefaultContainerName |
    Add-AzureHDInsightConfigValues -Hdfs $HdfsConfigValues

New-AzureHDInsightCluster -Name $ClusterName -Config $Config -Location $ClusterLocation -Credential $HdInsightCreds -Version $ClusterVersion
```

### How to deploy storm topology written in java to azure?

1.	Create myStormApp.jar file that includes the topology and dependencies

2.	Copy myStormApp.jar to the HdInsight storm head node

3.	Start storm command line and run:

```
storm jar myStormApp.jar  com.mycompany.myStormApp1.MyTopology InstanceName
storm jar c:\hanz-1.0.jar storm.blueprints.chapter1.v1.StormhdfsTopology stormhdfs
storm jar c:\ TemperatureMonitor.jar  com.microsoft.examples.Temperature temp
```

To Stop storm command:

```
Storm kill wordcount
```

## Logging and Performance Monitoring with Storm

### How to do performance monitoring in storm?
Storm UI provide real time performance result.

1.	Connect to storm head node

2.	Start Storm UI

3.	We can see:

-	perf summary for the cluster, topology, supervisor
-	Individual topology, spouts, bolts,
-	Configuration for nimbus and individual topology

## How to do performance monitoring in trident?

You use the same Storm UI for viewing performance.
Trident automatically convert your workflow into bolts and spouts

- MasterBatchCoordinator: generic for all trident topologies;
- spout coordinator: specific to your type of spout.

A trident "spout" is actually a storm bolt.

### How do we log in storm?

Storm topologies and topology components should use the [slf4j]( http://www.slf4j.org/) API for logging.

Add mvn dependency to slf4j

```
  <dependency>
    <groupId>org.slf4j</groupId>
    <artifactId>slf4j-api</artifactId>
    <version>1.7.7</version>
  </dependency>
```

Add logging code in your Bolt/Spout class:

``` java
Logger logger = (Logger) LoggerFactory.getLogger(MyBolt.class);
logger.info("My Log String");
```

### How to view logs in storm?
Steps:

1.	Log in to the head node

2.	Start stormUI

3.	Click on your topology

4.	Click on your spout or bolt

5.	Click on the port for an executors

6.	You should see the log result

### How to disable logging in storm?

Storm has its own logging. By default, logging is enabled.  
To disable logging:

``` java
TopologyBuilder builder = new TopologyBuilder();
builder.setSpout(..);
builder.setBolt(..);
Config conf = new Config();
conf.put(Config.TOPOLOGY_DEBUG, false);
LocalCluster cluster = new LocalCluster();
cluster.submitTopology("topologyName", conf, builder.createTopology());
```

## Use Azure Redis Cache in java

### How to install and run Redis on windows?

1.	Download [Redis on Windows]( https://github.com/MSOpenTech/redis)

2.	Start visual studio and open the redis-2.8\msvs\RedisServer.sln

3.	Build the solution

4.	start the program

### How to include Redis jar to maven?
Add the following dependency to your POM

```
<dependency>
  <groupId>redis.clients</groupId>
  <artifactId>jedis</artifactId>
  <version>2.6.0</version>
</dependency>
```

### How to connect to Azure Redis Cache from java

- Clone Java’s [Jedis Fork with support to SSL](https://github.com/RedisLabs/jedis)

- Run:

```
mvn install -Dmaven.test.skip=true
```

- Add the following to you pom

```
<dependency>
   <groupId>redis.clients</groupId>
   <artifactId>jedis</artifactId>
   <version>2.5.0-SNAPSHOT</version>
</dependency>
```

- Java Test Code:

``` java
  public static void main(String[] args) {
    Jedis jedis = new Jedis("MyAzureRedisCacheName.redis.cache.windows.net", 6380, 3600, true); //host, port, timeout,isSSL
    jedis.auth("MyAzureRedisCacheKey"); //auth with the key to the azure redis cache
    jedis.connect();
    if (jedis.isConnected()) {
      jedis.set("firstName", "My First name");
      System.out.println("firstName:: " + jedis.get("firstName"));
      jedis.lpush("citis", "San Fransisco");
      jedis.lpush("citis", "New Your");
      jedis.lpush("citis", "Seattle");
      List<String> citis = jedis.lrange("citis", 0, 2);
      for (String city : citis) {
        System.out.println(city);
      }
    } else {
      System.out.println("connection error");
    }
    jedis.close();
  }
```
