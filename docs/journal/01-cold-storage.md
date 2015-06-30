# Journal Entry #1
_Capturing event data and saving it in its raw format to cold storage_

:pencil2: working draft

> The purpose of this phase is to determine an approach to ingesting data, performing the simplest of processing, and saving it for subsequent analysis. Because the customer requires all event data to be stored indefinitely, the volume of data held in cold storage could become very large. Cold storage must therefore be inexpensive.

# Fabrikam's Perspective
Fabrikam's engineering team has very little experience with high-scale event-oriented systems. They want to make sure that they are [asking the right questions][orientation]. They also need to get a minimum viable product deployed.

For their [first milestone][milestone] they want to:
- Ingest a representative set of simulated events.
- Store the raw data for all events for later analysis. [:arrow_right:][cold-storage]
- Increase the scale until they match the targets for their customer [:arrow_right:][increase-scale]

They need a basic implementation that maps to the highlighted components of the logical architecture:

![plan for this first milestone](media/01-cold-storage/logical-architecture-for-milestone01.png)


## Devices

Initially, Fabrikam don't have access to the buildings, so they have elected to build a simple simulator that emits temperature events representing the readings for each device. These events are generated at a realistic rate, and the simulator can be configured to imitate a variable number of devices. The simulator has already been developed using the .NET Framework. A console application provides a simple user interface, but it is also possible to host the simulator as a web job to provide an environment for long-lived, unattended running.

> ![Markus](media/PersonaMarkus.png) 
"We invested a significant amount of time and effort in creating the simulator. It is important that we are able to simulate a realistic number of devices sending events at the expected rate."

## Event Processing

Per the customer requirements, the system needs to capture event information emanating from 100,000 devices, each device sending 1 reading every minute. This equates to approximately 1,667 events per second. The Event Processing component must be capable of capturing this volume of information without introducing any backlog into the system; events raised by devices could be time critical.

The system must also be capable of provisioning and deprovisioning devices. This process is two-stage; the physical device must be installed, and then it must be registered with the Event Processing component. Events from unregistered devices should be ignored.

The event data stream must also be protected. This means not only securing the content of the events, but actually hiding or camouflaging the event stream itself. For example, if an observer can see that motion detectors in a house are not registering any events then there is probably nobody home.

There are several ways you can implement the Event Processing component. The developers at Fabrikam considered the options described in the following sections:

### Build a cloud service that receives events posted as HTTP requests

The cloud service could be implemented as a web role that parses and saves the event information to cold storage. The cloud service effectively performs the ingestion and processing steps described in [Introducing the Journey][00-intro]

**Pros:**
- This approach is technically straightforward and quick to prototype

**Concerns:**
- **Throughput.** All incoming requests are handled by the HTTP.sys driver (the entry point into the IIS services stack), and are queued for processing by the IIS service. Worker threads running application code pick up these requests and handle them. The thread pool must be of sufficient size to prevent requests from backing up in the IIS queue (and possibly timing out, causing the request to be rejected), and the processing performed must run quickly enough to satisfy the time requirements for recording event information. To reduce these concerns, it is possible to scale the system by adding more instances. The Azure load-balancer can transparently direct requests to ensure an even distribution. However, there is a limit of 25 role instances per deployment. For a system of the size anticipated by the customer this should be fine, but other customers may have requirements that require many more instances; Fabrikam don't want to have to radically redesign the system in the future.

> ![Poe](media/PersonaPoe.png) 
"We briefly considered building a set of custom virtual machines running an open source web server in the cloud to see whether we could optimize it specifically for this task. We know that you can distribute the load across VMs by configuring load balancing with Azure. However, I asked the developers whether they really wanted Fabrikam to have to take responsibility for maintaining and patching these VMs and the web server software. Additionally, Gary was concerned about the security aspects of this approach - hardening a custom web server is a complex task. We decided against this approach pretty quickly."

- **Connectivity**. Many simple devices just emit signals that indicate status and don't necessarily send out HTTP packets. Additionally, those devices that can create HTTP messages are unlikely to all use the same schema. It may be necessary to incorporate local field gateways to perform message translation and formatting.

> ![Markus](media/PersonaMarkus.png) 
"Many devices are designed to be simple; they output signals indicating events that have occurred, and they can receive signals that indicate commands to be performed. They might not be capable of translating this data into different formats for integration into an IoT solution; this process typically requires some auxiliary hardware and/or software."

![cloud service solution based on web roles](media/01-cold-storage/physical-architecture-cloud-service.png)

- **Resilience and reliability.** If an instance of the service fails, then any messages that it is processing might be lost.

- **Scalability.** Event data might need to be handled in multiple ways, such as copying to cold storage, performing warm analysis and aggregation, or for raising alerts. If all of these tasks become the responsibility of the cloud service it might not be capable of handling this work together with the large ingress of event information.

- **Safety and security.** *THOUGHTS - How easy/difficult is it to protect the event data? How prone is a service to attack? What are the attack surfaces? TBD*

> ![Gary](media/PersonaGary.png) 
"It is not just the contents of event data messages that require protection, but also the endpoints that are sending and receiving this data. If you can infiltrate an endpoint, perhaps by sending a spoof command to a device or rogue messages to a service, then you may be able to gain control of the network."

- *Other concerns - runtime costs, ease of deployment, ...*

### Use a Service Bus topic to ingest events and worker role instances to process them

In this configuration, devices post event data to a Service Bus topic. Worker role instances can subscribe to this topic to retrieve and process the event information. 

**Pros:**

- **Reduced bandwidth** Devices can send event messages to Service Bus using the [AMQP][AMQP] protocol. This protocol is very efficient, using a binary encoding for messages.

- **Throughput.** A worker role has less overhead than a web role (*NEED SOMEONE TO VERIFY THIS, AND MORE DETAIL ON WHY*)

- **Reliability.** The AMQP protocol supports reliability guarantees, helping to ensure that event data is not lost when sent to a topic. Additionally, a subscription can be transactional; if a worker role fails while processing the data for an event this data is not lost but is instead returned to the Service Bus topic from whence it can be retrieved again.

- **Scalability.** Different worker roles could be created to handle different types of event processing (copying to cold storage, performing warm analysis and aggregation, raising alerts).

- **Security**. The worker role has a reduced attack surface. It only retrieves messages from a well-known subscription and does not accept unsolicited incoming traffic. Additionally, you can set security policies to help protect the Service Bus topic and authenticate message senders. AMQP provides transport-level security to protect messages in-flight.

> ![Gary](media/PersonaGary.png) 
"A service that pulls data from a trusted source is easier to protect than a service that accepts push requests from the outside world."

**Concerns:**

- **Scalability.** There is a limit of 25 instances per cloud service deployment, leading to the same scalability concerns described in the previous approach.

- **Safety and Security.** Service Bus endpoints must be protected to prevent rogue messages from being posted. *Others?*

- **Connectivity.** Devices need to be able to communicate with Service Bus in order to post messages. As before, local field gateways may be necessary to provide this connectivity.

![cloud service solution based on Service Bus Topics and Work Roles](media/01-cold-storage/physical-architecture-worker-role-and-service-bus.png)

- *Other Concerns - TBD*

### Use Azure Event Hub to ingest events and a worker role to process them

[Azure Event Hub][event-hubs] is a cloud-scale telemetry ingestion service. It is designed to capture millions of events per second in near real-time.

**Pros:**

- **Reliability.** As described in the previous approach, the AMQP protocol provides reliability guarantees, helping to ensure that event data is not lost prior to ingestion. Data received by Event Hub is retained for a specified period (which can be measured in days). If a worker role fails when processing the data for an event, it can be restarted and data is not lost.

- **Scalability.** Event Hub is highly scalable through partitioning. An event hub can contain up to 32 partitions; each partition can receive messages in parallel with other partitions. Event Hub is designed to handle a continuing, large influx of events and is capable of processing up to 1MB/second of data per partition; this is well beyond the current requirements of the system that Fabrikam are proposing and allows the same architecture to be used for much larger systems in the future.

- **Security.** You can set security policies to help protect Event Hub and authenticate message senders. AMQP provides transport-level security to protect messages in-flight. Event Hub also supports blacklisting of devices; if a device is compromised or stolen, data transmitted by the device can be blocked on receipt by Event Hub.

**Concerns:**

- **Immaturity.** This is a new technology and the developers at Fabrikam need to invest time in learning its capabilities and how to use them.

- **Complexity.** Receiving data from Event Hub is a significantly different process from retrieving a message from a Service Bus queue. The [Event Hubs Programming Guide][event-hubs-programming-guide] contains the details.

> ![Jana](media/PersonaJana.png) 
"Getting to grips with new technology always take time, and sometimes requires a few iterations to understand how to use it properly."

- **Throughput.** Event Hub is priced in terms of throughput units. A throughput unit specifies the rate at which data can be sent and received by using Event Hub. If an application exceeds the number of purchased throughput units, performance will be throttled and may trigger exceptions. The DevOps team must constantly monitor the event hub to ensure that sufficient throughput units are available.

- **Scalability.** Although Event Hub is highly scalable, the same concerns with the scalability of worker roles outlined in the previous approach are still present.; will they be able to keep up with the outflow of data from Event Hub?

![cloud service solution based on Event Hub and Work Roles](media/01-cold-storage/physical-architecture-worker-role-and-event-hub.png)

### Use Azure Event Hub to ingest events and Azure Stream Analytics to process them

[Azure Stream Analytics][stream-analytics] is a real-time stream processing service. It can capture incoming streams of data from many sources, combine them, and arrange for these streams to be processed and send the results to one or more destinations.

**Pros:**

- **Ease of Use:** Stream Analytics uses a declarative model for specifying the input and output streams, and the transformations to be performed by the processing. It can gather data directly from Event Hub as well as sources such as Blob storage, and it can emit data to Event Hub, Blob storage, Table storage, and Azure SQL Database.

- **Scalability:** As with Event Hub, Stream Analytics is designed to be highly scalable, capable of supporting event handling throughput of up to 50Mb/second. Stream Analytics will automatically scale based on the event ingestion rate, complexity of processing, and expected latencies.

- **Reliability:**  The Stream Analytics service is built to persist state and cache output efficiently. These features provide fast recovery from processing node failures, quickly reprocessing lost state.

**Concerns:**

- **Immaturity.** As with Event Hub, this is a new technology. The developers at Fabrikam need to invest time in learning its capabilities and how to use them.

- **Event Metadata.** The event stream passed to Stream Analytics does not include the event metadata that is captured by Event Hub. This metadata can be valuable and can include information not available in the main payload of the event data. If this metadata is required, it might be necessary to connect directly to Event Hub and use an alternative approach to Stream Analytics.

### Event Processing - Selected Technologies

Fabrikam decided to use Event Hub and Stream Analytics to ingest and process event data. Although these technologies are very new, the scalability, reliability, and securability (the ability to verify and protect event information as it is received), and ease of use swung the decision; the maintenance and monitoring costs of this solution are far less than those concerned with using web and worker roles.

***TODO: Would be good to show table of costs, comparing web/work roles to EH, Stream Analytics***

The simulator (and later on, the real devices) will send events directly to Event Hub. A Stream Analytics job will fetch the data for every event and send it to cold storage. The data for each event will be saved as a line-delimited JSON object for maximum interoperability.

Fabrikam also decided to implement a secondary batch processor for handling events directly from Event Hub. This batch processor has access to the event metadata that is not available to Stream Analytics and can perform additional processing based on this information. The results are also directed towards cold storage.

![cloud service solution based on Service Bus Topics and Work Roles](media/01-cold-storage/physical-architecture-stream-analytics-and-event-hub.png)

## Cold Storage

Have elected to use Event Hub and Stream Analytics, the next decision that Fabrikam had to make concerned the storage to use for holding the event data after it has been received. The choice of Stream Analytics narrows the choice of storage technology to Event Hub, Azure SQL Database, Table storage, and Blob storage.

Using Event Hub as a destination was quickly discounted due to the requirement to store data indefinitely; Remember that the primary requirements are that cold storage has to record an indeterminate amount of data very quickly and for an indefinite period. The following sections briefly summarize the discussions that the developers had concerning the remaining possibilities.

### Azure SQL Database

[Azure SQL Database][sql] is an excellent choice for storing structured data that can be easily queried by using SQL. Azure SQL Database is priced according to the service tier/performance level selected. Each service tier/performance level provides different performance and storage capabilities, ranging from 16,600 transactions per hour (4.6 transactions per second) and a 2GB database for the basic performance level up to 735 transactions per second and 500GB of database storage at the top end. This may not be sufficient to record the details for the anticipated 1667 events per second (assuming that the data for each event is passed to storage as a single operation by Stream Analytics). Additionally, although 500GB sounds significant it only provides storage for a finite amount of data; if each event record consumes 20 bytes of storage (estimated), then a 500GB database can save data for approximately 175 days worth of events. Also, 1667 events per second is purely the baseline that Fabrikam expect for their first implementation. In subsequent rollouts the volume might increase by orders of magnitude. 

There is one further point to consider. Databases are held on a shared database server infrastructure which might be utilized by other clients. Although databases are protected from each other to prevent accidental exposure of data, the infrastructure has to ensure that resources are balanced carefully. You purchase database resources in terms of Database Throughput Units (DTUs). DTUs are based on a blended measure of CPU, memory, reads, and writes. If an application exceeds its quota of DTUs it will be throttled.

> ![Beth](media/PersonaBeth.png) 
"The operational costs of using Azure SQL database for this solution could be excessive given the throughput and storage requirements. We simply don't need all of the rich features that Azure SQL Database provides to store event data; the information is not relational and we just want to save the data quickly and efficiently."

> ![Poe](media/PersonaPoe.png) 
"Azure SQL Database is not ideally suited to chatty applications or systems that perform a large number of data access operations that are sensitive to network latency." 

### Table Storage

[Table storage][table-storage] is a key/value store that provides a suitable environment for saving large volumes of structured data. A table stores multiple rows, each of which can be up to 1MB in size, and you can store up to 500TB of data in a table (this equates to approximately 950 years of event data if each record is 20 bytes in size). The documented [scalability targets][storage-scalability-targets] for Azure Storage specify that the system should be able to handle up to 20000 messages per second (this is for messages that are 1KB in size), and a total inbound bandwidth of between 10 and 20GB per second.

You should consider using Azure Table Storage when:

- Your application must store significantly large data volumes (expressed in multiple terabytes) while keeping costs down.

- Your application stores and retrieves large data sets and does not have complex relationships that require server-side joins, secondary indexes, or complex server-side logic.

- Your application requires flexible data schema to store non-uniform objects, the structure of which may not be known at design time.

- Your business requires disaster recovery capabilities across geographical locations in order to meet certain compliance needs. Azure tables are geo-replicated between two data centers hundreds of miles apart on the same continent. This replication provides additional data durability in the case of a major disaster.

- You need to store more data than you can hold by using Azure SQL Database without the need for implementing sharding or partioning logic.

- You need to achieve a high level of scaling without having to manually shard your dataset.

> ![Beth](media/PersonaBeth.png)
"Using Table storage to hold information about device events should be cost effective. Table storage is relatively cheap compared to some other options."

> ![Jana](media/PersonaJana.png)
"Table storage is fast, but if we simply want to blast the data into a data store for later processing do we really need to save it in a structured manner? The data is being provided from Stream Analytics as a JSON string, so the quickest way to save that information would be in its native format rather than parsing it into a set of fields and allocating a unique key for each record."

### Blob Storage

[Blob storage][blob-storage] enables you to store large amounts of unstructured information, such as text or binary data, quickly and efficiently. It doesn't provide the search and filtering capabilities of table storage, but is ideal for saving a high-volume stream of data. Each stream can be handled and named like a file, and a new stream could be created each for working day (or for each hour, possibly). The scalability targets are the same as for table storage; Fabrikam should be able to store up to 950 years worth of data at a rate of up to 20000 records per second.

### Cold Storage - Selected Technology

Fabrikam selected Blob storage as the cold storage technology. Stream Analytics outputs the event data as a JSON formatted string which is streamed to a file held in Blob storage. This choice meets their requirements for throughput and capacity, and has relatively low costs. Data durability in the event of a disaster can be guaranteed by configuring blobs to use geo-replication across data centers.

> ![Carlos](media/PersonaCarlos.png) 
"While it might have been useful to store the event data in a format that makes it easy to analyze, I am satisfied that I can use BI tools to retrieve the data from Blob storage and examine it offline." 

## Lessons Learned - What Fabrikam discovered

The developers uncovered the following issues when using Stream Analytics to save data to blob storage:
- HDInsight Hive queries will fail when running against data held in blob files being actively written to by Stream Analytics. It is possible to workaround this problem by ignoring blob access exceptions.

- HDInsights requires the data held in blob storage to be JSON formatted with each record on a new line. This requires configuring the Stream Analytics output format appropriately.

*TODO Others?*


[00-intro]: .\00-introducing-the-journey.md
[traffic-manager]: https://azure.microsoft.com/documentation/articles/traffic-manager-overview/
[AMQP]: https://www.amqp.org/
[event-hubs-programming-guide]: https://msdn.microsoft.com/library/azure/dn789972.aspx
[sql]: http://azure.microsoft.com/en-us/services/sql-database/
[table-storage]: https://azure.microsoft.com/documentation/articles/storage-dotnet-how-to-use-tables/
[storage-scalability-targets]: https://azure.microsoft.com/documentation/articles/storage-scalability-targets/
[blob-storage]: http://azure.microsoft.com/documentation/articles/storage-dotnet-how-to-use-blobs/
[event-hubs]: http://azure.microsoft.com/services/event-hubs/
[stream-analytics]: http://azure.microsoft.com/services/stream-analytics/
[milestone]: https://github.com/mspnp/iot-journey/milestones/Milestone%2001
[orientation]: https://github.com/mspnp/iot-journey/issues/20
[cold-storage]: https://github.com/mspnp/iot-journey/issues/26
[increase-scale]: https://github.com/mspnp/iot-journey/issues/30