# Writing Event Data to Long-Term Storage

This topic explores options for writing raw event data into long-term storage. 

- [Requirements](#requirements)
- [Exploring solutions](#exploring-solutions)
- [Reference implementation](#reference-implementation)
- [Lessons learned](#lessons-learned)

## Requirements

- Long-term storage must hold a large volume of historical data indefinitely, at a reasonable cost. 
- It must also handle the expected rate of incoming event data.
- Other factors, such how easy it is to query the data, are secondary.

> Long-term storage is sometimes called *cold storage*, although traditionally "cold storage" meant tape backups. 

## Exploring solutions 

We looked at the following options:

- Azure SQL database
- Azure Table storage
- Azure Blob storage

This list was motivated in part because Azure Stream Analytics can write to all of them (see the [Event ingestion] journal entry).

### Azure SQL Database

[Azure SQL Database][sql] is an excellent choice for storing structured relational data. However, you must carefully consider performance and size requirements. 

Azure SQL Database is priced by service tier. Each tier provides different performance and storage capabilities, ranging from 5 transactions per second and a 2GB database, up to 735 transactions per second and 500GB of storage at the top end. 

Even at the highest tier, the throughput might not be enough to record our target of ~1667 events per second, assuming that Stream Analytics sends each event as a single operation. And even though 500GB sounds large, it can fill up. If each event record requires 20 bytes of storage, a 500GB database can hold approximately 175 days' worth of events.  

Because databases are held on a shared database server, the infrastructure has to ensure that resources are balanced carefully. You purchase database resources in terms of Database Throughput Units (DTUs), which are based on a blended measure of CPU, memory, reads, and writes. If an application exceeds its quota of DTUs it will be throttled.

> Azure SQL Database is not ideally suited to chatty applications or systems that perform a large number of data access operations that are sensitive to network latency. 

The operational costs of using Azure SQL database for this solution could be excessive, given the throughput and storage requirements. Moreover, the Fabrikam scenario simply doesn't need all of the rich features of a relational database. The event data is not relational; we just want to save it quickly and efficiently.


### Azure Table storage

[Table storage][table-storage] is a key/value store that can save large volumes of structured data. A table stores multiple rows, each up to 1MB in size, and you can store up to 500TB of data in a table &mdash; approximately 950 years of event data if each record is 20 bytes. The documented [scalability targets][storage-scalability-targets] for Azure Storage specify that the system should be able to handle up to 20000 messages per second  for messages that are 1KB in size, and a total inbound bandwidth of between 10 and 20GB per second.

You should consider using Azure Table storage when:

- Your application must store significantly large data volumes (multiple terabytes) while keeping costs down.

- Your application stores and retrieves large data sets and does not have complex relationships that require server-side joins, secondary indexes, or complex server-side logic.

- Your application requires a flexible data schema to store non-uniform objects, the structure of which may not be known at design time.

- Your business requires disaster recovery capabilities across geographical locations in order to meet certain compliance needs. Azure tables are geo-replicated between two data centers hundreds of miles apart on the same continent. This replication provides additional data durability in the case of a major disaster.

- You need to store more data than you can hold by using Azure SQL Database without the need for implementing sharding or partioning logic.

- You need to achieve a high level of scaling without having to manually shard your dataset.

> Table storage should be cost effective, because it is relatively cheap compared to some other options. It is also fast. But if we simply want to blast the data into a data store for later processing, do we really need to save it as structured data? The data is being provided from Stream Analytics as a JSON string, so its quicker to save that information in its native format rather than parse it into a set of fields and allocate a unique key for each record.

### Azure Blob storage

[Blob storage][blob-storage] can store large amounts of unstructured information, such as text or binary data, quickly and efficiently. It doesn't provide the search and filtering capabilities of Table storage, but is ideal for saving a high-volume stream of data. Each stream can be handled and named like a file, and a new stream could be created each for working day (or for each hour, possibly). The scalability targets are the same as for Table storage; Fabrikam should be able to store up to 950 years worth of data at a rate of up to 20000 records per second.


## Reference implementation

For the reference implementation, we picked **Blob storage**. Stream Analytics outputs the event data as a JSON formatted string which is streamed to a file held in Blob storage. This choice meets our requirements for throughput and capacity, and has relatively low costs. Data durability in the event of a disaster can be guaranteed by configuring blobs to use geo-replication across data centers.

> While it might be useful to store the event data in a format that makes it easy to analyze, BI tools can retrieve the data from Blob storage and examine it offline. 



[Event ingestion]: 04-event-ingestion.md
[sql]: http://azure.microsoft.com/en-us/services/sql-database/
[table-storage]: https://azure.microsoft.com/documentation/articles/storage-dotnet-how-to-use-tables/
[storage-scalability-targets]: https://azure.microsoft.com/documentation/articles/storage-scalability-targets/
[blob-storage]: http://azure.microsoft.com/documentation/articles/storage-dotnet-how-to-use-blobs/

