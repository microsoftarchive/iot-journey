# Storm

What would it take to implement the requirements from #47 using Storm.

1. calculate the average temperature for each building over a five minute window.

2. output the result to SQL.

3. Deploy the topology to HDInsight

# Implementation 1: Trident Solution with EventHub Trident Spout

## Eventhub partitioned by buildingID or not:

1. Use case 1: The event sent to eventhub does not contain buildingID or is not partitioned by buildingID: In that case, the thermal stats in a specific building may send their event to a different event hub partitions. To calculate the average temperature for a building, we need to do cross partition aggregation.

2. Use case 2: The event sent to eventhub is partitioned by buildingID: In that case, all the thermal stat in a specific building will send it's event to a same event hub partition. To calculate the average temperature for a building, we don't need to do any cross partition aggregation. Partition-local operations will suffice. One implementation would be use an Aggregator.

## Exactly-once/at-most-once/at-least-once:
In case the business focus on on-time result of latest average building temperature, we may not require exactly-once scenario. a replay may not be required in case of failure.

## How to implement the time window
For Trident, the event is already batched. A straight forward way would be set the event hub spout so that all the messages within the 5 minute window to be send in the same batch. However, the event hub trident spout batch settings is based on batch size not the time. It might not be possibe that each batch will be exactly 5 minutes. So the output from the topology will not be exactly in 5 minute windows. It may very.

To ensure that the output is exactly in 5 minute window, we need to implement more complex logic. One way would be buffer the event and do the aggregation once we have received all the event with the time window.

The best and simplest situation is to display the average temperature for a batch, and the time duration on that batch is not fixed.

## How to join the event with the reference data (buildingID vs deviceID) in case that buildID is not in the event data
We can use the trident Merges and joins feature. trade event data as stream1, and reference data (buildingID vs deviceID) as stream2

```
topology.join(stream1, new Fields("dvcID"),
stream2, new Fields("dvcID"), new Fields("dvcID", "bldID", "temp", "ObservedTime"));

```

## How to write to Azure SQL Database

[Storm JDBC](https://github.com/apache/storm/tree/master/external/storm-jdbc) provide
Storm/Trident integration for JDBC. This package includes the core bolts and trident states that allows a storm topology to either insert storm tuples in a database table or to execute select queries against a database and enrich tuples in a storm topology.


# Implementation 2: Storm Solution with EventHub Spout

## How to put the event from the same build together
Use "buildingID" [field grouping](https://storm.apache.org/documentation/Concepts.html), tuples with the same "buildingID" will always go to the same task, but tuples with different "buildingID"'s may go to different tasks.

## How to put implement the time window
After the event have been group, use bolt to buffer the input stream with the specified time window. Once the time window is full, calculate the average and emit the result.

## How to write to the azure database.
use [Storm JDBC](https://github.com/apache/storm/tree/master/external/storm-jdbc)

# Implementation 3: Storm Solution with EventHub Spout and flowmix Tumbling Window

Use calrissian flowmix (https://github.com/calrissian/flowmix) tumbling Window. there is a Maven Repo for flowmix (http://mvnrepository.com/artifact/org.calrissian.flowmix). This might save us some time since we don't need to develop our Tumbling Window logic.

# Implementation 4: Storm Solution with tick tuples

Details see: http://www.michael-noll.com/blog/2013/01/18/implementing-real-time-trending-topics-in-storm/
