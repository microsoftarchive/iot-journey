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
"We spent a significant amount of time creating the simulator, and it was important that we were able to simulate a realistic number of devices sending events at the expected rate."

## Event Processing

Per the customer requirements, the system needs to capture event information emanating from 100,000 devices, each device sending 1 reading every minute. This equates to approximately 1,667 events per second. The Event Processing component must be capable of capturing this volume of information without introducing any backlog into the system; events raised by devices could be time critical.

There are several ways you can implement this component. The developers at Fabrikam considered the options described in the following sections:

### Build a cloud service that receives events posted as HTTP requests

The cloud service could be implemented as a web role that parses and saves the event information to cold storage. The cloud service effectively performs the ingestion and processing steps described in [Introducing the Journey][00-intro]

This approach is technically straightforward and quick to prototype, but the developers had some concerns about the viability of this approach, including:

- Is it fast enough? All incoming requests are handled by the HTTP.sys driver (the entry point into the IIS services stack), and are queued for processing by the IIS service. Worker threads running application code pick up these requests and handle them. The thread pool must be of sufficient size to prevent requests from backing up in the IIS queue (and possibly timing out, causing the request to be rejected), and the processing performed must run quickly enough to satisfy the time requirements for recording event information. To reduce these concerns, it is possible to scale the system by adding more instances, but they are not sure whether the additional redirection that this implies will slow the system down too much.

- Is it robust? *THOUGHTS - Failure detection and handling in an instance of the service. What happens if an instance fails while processing event data? Is that data lost? TBD?*

- Is it safe? *THOUGHTS - How easy/difficult is it to protect the service and event data? How prone is a service to attack? What are the attack surfaces? TBD*

- *Other concerns - runtime costs, ease of deployment, ...*

### Use a Service Bus topic to ingest events and worker role instances to process them

In this configuration, devices post event data to a Service Bus topic. Worker role instances can subscribe to this topic to retrieve and process the event information. This approach is more complex than the the previous one but has several advantages, including:

- Speed. A worker role has less overhead than a web role (*NEED SOMEONE TO VERIFY THIS, AND MORE DETAIL ON WHY*)

- Reliability. A subscription can be transactional. If a worker role fails while processing the data for an event this data is not lost but is instead returned to the Service Bus topic from whence it can be retrieved again.

Despite these advantages, the developers had some concerns, including:

- *TBD - List possible concerns* 

### Use Azure Event Hub to ingest events and a worker role to process them

*TBD - Reliability (AMQP). Scalability (partitioning). Security?*

### Use Azure Event Hub to ingest events and Azure Stream Analytics to process them

*TBD*

*DESCRIPTION*

 - [Event Hubs][event-hubs], a cloud-scale telemetry ingestion serivce.
 - [Stream Analytics][stream-analytics], a real-time stream processing service.
 




The decision was made to use Event Hub and Stream Analytics. The simulator (and later on, the real devices) will send events directly to Event Hub. A Stream Analytics job will fetch the data for every event and send it to cold storage. The data for each event will be saved as a line-delimited JSON object for ease of use.

*DIAGRAM*

## Cold Storage

*TBD For cold storage, we'll use [Blob storage][blob-storage]; a service for storing large amounts of unstructured data.*

## Lessons Learned - What Fabrikam discovered
> TODO List the insights, points of confusion, and challenges that we encountered during this step of the journey

[00-intro]: https://github.com/mspnp/iot-journey/docs/journal/00-introducing-the-journey.md


[sql]: http://azure.microsoft.com/en-us/services/sql-database/
[blob-storage]: http://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-how-to-use-blobs/
[event-hubs]: http://azure.microsoft.com/en-us/services/event-hubs/
[stream-analytics]: http://azure.microsoft.com/en-us/services/stream-analytics/
[milestone]: https://github.com/mspnp/iot-journey/milestones/Milestone%2001
[orientation]: https://github.com/mspnp/iot-journey/issues/20
[cold-storage]: https://github.com/mspnp/iot-journey/issues/26
[increase-scale]: https://github.com/mspnp/iot-journey/issues/30