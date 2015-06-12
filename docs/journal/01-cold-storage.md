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

Initially, Fabrikam don't have access to the buildings, so they have elected to build a simple simulator that emits temperature events representing the readings for each device. These events are generated at a realistic rate, and the simulator can be configured to imitate a variable number of devices. The simulator has already been developed using the .NET Framework and is implemented as a class library. A console application provides a simple user interface, but it is also possible to host the simulator in a cloud service to provide an environment for long-lived, unattended running.

> ![Markus](media/PersonaMarkus.png) 
"We invested a significant amount of time and effort in creating the simulator. It is important that we are able to simulate a realistic number of devices sending events at the expected rate."

## Event Processing

Per the customer requirements, the system needs to capture event information emanating from 100,000 devices, each device sending 1 reading every minute. This equates to approximately 1,667 events per second. The Event Processing component must be capable of capturing this volume of information without introducing any backlog into the system; events raised by devices could be time critical.

There are several ways you can implement this component. The developers at Fabrikam considered the options described in the following sections:

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
"Many devices are designed to be simple; they output signals indicating events that have occured, and they can receive signals that indicate commands to be performed. They might not be capable of translating this data into different formats for integration into an IoT solution; this process typically requires some auxiliary hardware and software."

![cloud service solution based on web roles](media/01-cold-storage/physical-architecture-cloud-service.png)

- **Resilience and reliability.** If an instance of the service fails, then any messages that it is processing might be lost.

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

[Azure Event Hub][event-hubs] is a cloud-scale telemetry ingestion service. It is designed to capture millions of events per second in near real time. Devices connections are brokered by using Azure Service Bus.

**Pros:**

- **Reliability.** As described in the previous approach, the AMQP protocol provides reliability guarantees, helping to ensure that event data is not lost prior to ingestion. Data received by event hub is retained for a specified period (which can be measured in days). If a worker role fails when processing the data for an event, it can be restarted and data is not lost.

- **Scalability.** Event Hub is highly scalable through partitioning. 

- **Security.** You can set security policies to help protect the event hub and authenticate message senders. AMQP provides transport-level security to protect messages in-flight.

**Concerns:**

- **Immaturity.** This is a new technology and the developers at Fabrikam need to invest time in learning its capabilities and how to use them.

- **Complexity.** Receiving data from an event hub is a significantly different process from retrieving a message from a Service Bus queue. The [Event Hubs Programming Guide][event-hubs-programming-guide] contains the details.

> ![Jana](media/PersonaJana.png) 
"Getting to grips with new technology always take time, and sometimes requires a few iterations to understand how to use it properly."

- **Throughput.** Event Hub is priced in terms of throughput units. A throughput unit specifies the rate at which data can be sent and received by using Event Hub. If an application exceeds the number of purchased throughput, performance will be throttled and may trigger exceptions. 

- **Scalability.** Although Event Hub is highly scalable, the same concerns with the scalability of worker roles outlined in the previous approach are still present.

![cloud service solution based on Event Hub and Work Roles](media/01-cold-storage/physical-architecture-worker-role-and-event-hub.png)

### Use Azure Event Hub to ingest events and Azure Stream Analytics to process them

*TBD*

*DESCRIPTION*

[Azure Stream Analytics][stream-analytics] is a real-time stream processing service.

**Pros:** *TBD*

**Concerns:** *TBD*


Fabrikam decided to to use Event Hub and Stream Analytics *TBD Why? What were the factor(s) that swung the decision this way?*. 

The simulator (and later on, the real devices) will send events directly to Event Hub. A Stream Analytics job will fetch the data for every event and send it to cold storage. The data for each event will be saved as a line-delimited JSON object for ease of use.

![cloud service solution based on Service Bus Topics and Work Roles](media/01-cold-storage/physical-architecture-stream-analytics-and-event-hub.png)

## Cold Storage

TBD - Follow same pattern as above:

Options: 
Blob storage (fast, cheap, but less easy to pose complex queries)
Azure SQL Database (more expensive, but easier to query)
Table Storage
Elastic Storage
Others?

## Lessons Learned - What Fabrikam discovered
> TODO List the insights, points of confusion, and challenges that we encountered during this step of the journey

[00-intro]: https://github.com/mspnp/iot-journey/docs/journal/00-introducing-the-journey.md
[traffic-manager]: https://azure.microsoft.com/documentation/articles/traffic-manager-overview/
[AMQP]: https://www.amqp.org/
[event-hubs-programming-guide]: https://msdn.microsoft.com/library/azure/dn789972.aspx


[sql]: http://azure.microsoft.com/en-us/services/sql-database/
[blob-storage]: http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/
[event-hubs]: http://azure.microsoft.com/en-us/services/event-hubs/
[stream-analytics]: http://azure.microsoft.com/en-us/services/stream-analytics/
[milestone]: https://github.com/mspnp/iot-journey/milestones/Milestone%2001
[orientation]: https://github.com/mspnp/iot-journey/issues/20
[cold-storage]: https://github.com/mspnp/iot-journey/issues/26
[increase-scale]: https://github.com/mspnp/iot-journey/issues/30