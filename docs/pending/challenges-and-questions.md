# Challenges & Questions

This is a list of challenges and questions surrounding IoT solutions.
This does not represent our project backlog. There's more here than we are likely able to cover. However, we believe that there is value even in just listing the challenges.

One prominent question that should be asked at all times and in all scenarios:

> What are the safety and security concerns?

## Device Management

_In this context, a device could also be a field gateway._

- How do I provision devices? That is, how do I authorize a specific device to interact with the backend? 
- How do I control permissions for how a device interacts with the backend? Can it connect? or receive commands? or send data?
- How do I offer provisioning services to third parties, while maintaining a secure system?
- How do I authenticate devices?
  - What does "device identity" really mean?
  - Should I rollover credentials on a timed basis? If so, how?
- How do I handle devices that are bad actors? Such as:
  - When a device is physically comprised.
  - When a device is misconfigured.
  - When a device is defective.
- How do I remotely update devices? (e.g., changes in firmware and configuration)
- How do I keep devices secure? Both physical security and cyber security.
- How do I encrypt or otherwise ensure the privacy of data coming from the device?
- How do I logically group devices? (so that they can be managed as a group)

## Device to Cloud Gateway Communication

_In this context, a device could also be a field gateway._

- What are the considerations when choosing a serialization format for the event body? What are the reasons for and against:
  - An off-the-shelf serializer? (e.g. Protobuf, Thrift, etc.)
  - Implementing a custom serializer?
- What are the considerations when choosing an application protocol? 
- When would implementing a custom application protocol be necessary?
- What data should be included in the serialized body of an event? In contrast to what data should be easily accessible in the meta-data (e.g., a high priority flag)?
- How are schemas for serialized event data managed across different portions of the system? Meaning, how do we ensure that data serialized using C on a device is properly deserialized in C# in the cloud?
- How do we manage updating an existing schema (possibly when new devices are deployed)? What are the considerations of versioning schema?
- How do I avoid extra latency when routing events through the event broker? For example, ensuring that high priority events meet a certain SLA.
- What is the impact of the size of events on the cloud gateway?
- How do I decide whether to send device data as events in real time, or aggregated and uploaded as a batch?
- What are the implications of sending events in a batch to the cloud gateway?
- How can I support an "ABC" schema, where all events share a common A schema but vary by BC? Does this have an effect on the gateway?
- How and when do I normalize the data? (deadbanding, debouncing, deduplication and A2D conversions)
- How do I handle device-specifc data? (e.g., perform I/O mapping)
- How do I appropriately partition the gateway to avoid performance issues (hot partitions)?
- How do I handle high priority messages (e.g. emergency or distress call)?
- How do I handle temporal aggregation and triggering of virtual events?
- How should I handle time synchronization across the devices, etc. (deviation from gateway)?
- How do can I know the "last known state" for any given device?
- How do I handle malformed events? That is, what happens when the backend doesn't understand what the device sent?
- How can I support multiple protocols in my system?
- How do I audit connections?
- What if my endpoint address changes?
- How do I know the device message was not tampered with?

## Cloud Gateway to Device/Field Gateway Communication

_In this context, a device could also be a field gateway._

- How do I send a command to a device? What if the device is only occasionally connected?
- How do I broadcast to multiple devices?
    - Broadcasting to a logical group?
    - Broadcast to a dynamic group (result of a query)?
- How do I ensure that the device received and processed the command?
- How does the cloud gateway keep a connection open to the device so that it can send outbound messages?
- How can I communicate with a device that is not currently connected to the service?
- What should I do if a device doesn't respond to a command in a timely manner?
- How can I ensure that a command was not tampered with in transit?

## Real-time stream processing (complex event processing)

_There’s a lot of overlap here with other areas._

- When processing "data-in-motion", how do I normalize the events in my system?
    - Filtering out anomalies or unwanted events
    - Normalize with respect to different schema
- How do I perform time-based analytics on a stream, such as detecting patterns, errors, and aggregates? (e.g., if 3 cars skid on a stretch of road within a specified window of time, other cars need to be warned about the road condition)
- How do I aggregate events that generate a "logical event"?
- Are translations (projections) done before, after, or as a part of real-time stream processing?
- What parts of my business logic should be handled in real-time?
- What are the trade-offs between real-time processing and post processing?
- What do we really mean by "real-time"? There are multiple definitions that mean very different things. (A real-time OS is very different from real-time stream processing.)
- How do I deal with the possibility of events processed out of order?  

## Stateful Processing

- How do I prevent concurrent processing against a state from corrupting the state?
- How do I persist state long term (e.g. for a long-running service, where is the state stored)?
- How can I release resources for a stateful service when it is not actively processing?
- How do I scale out stateful services (e.g. avoid affinity for a specific node and load balancing)?
- How do we trigger time-based processing (e.g. set temperature by schedule or by extreme high or low outside temperature)?
- How do we scale these models across multiple servers?
- How can I query for a state at a given point in time (or time range)?

## Long-Term Storage (Cold Storage)
- How do we store all of the event data effectively?
- What are appropriate storage technologies? blobs, DocumentDB, HDFS, Elasticsearch, etc.?
- Do I need to run a Hadoop cluster to write event data to cold storage?
- How should I transform my event data in order to efficiently store it in blobs?
    - How do I append data to the blobs?
    - How do I roll over when a blob is "full"?
- How should I partition incoming events before writing them to blobs?
    - Should events with different schemas be stored together in a single blob?
    - Should I partition by date/time?
    - Do I need a "temporary staging storage for events"?
    - Should I try to optimize for anticipated queries?
    - Should I use multiple storage accounts?
    - Should I use the blob leasing mechanisms?
- What are the considerations when choosing a serialization format for long term storage?
    - How does this impact storage cost?
    - How does this impact query cost and performance?
    - Do I need to store multiple formats for different analytics tools, or is there a specific format that all can use?
- What do I do when my throughput exceeds the capacity of my chosen storage technology?
    - How to handle when it exceeds the throughput limits of a single storage account?
- How do I associate the schema with events persisted to long-term storage?
    - Is the schema embedded once per "file"?
    - Is the schema embedded with each event?
- How do I ensure an individual event is only written once to long-term storage?
    - What identifies a unique event, so I can track the event from the device to long-term storage?
- What are the trade-offs between cost of persistence and cost of access? (It may be cheap to store, but expensive to access/query.)
- Should I persist events of differing schema into a single blob, followed by immediate post-processing to support an easier-to-query logical partitioning of the data?
- Do I need to store data in time series?
- What serialization format should I initially use to store events so they can easily be transformed to a different format if I need to query?

## Analytics
- How do I make data (perhaps from long-term storage) available for HDInsight?
- How can we leverage metadata (schema or event type) in the system to support productivity during analysis (ad-hoc or otherwise)?
- How do I query a data set that contains multiple versions of a schema?
- What kind of scale support can I expect from the analytics tools?
- What costs can I expect for each analytic service?

## Instrumentation & System Health
- What are the KPI that should be instrumented for the various aspects of the system?
    - Devices?
    - Cloud Gateway?
    - Down-stream event processors?
- How do I automate load shedding when the KPI is bad?
- How can I monitor the overall system health?
- How do I determine the overall cost? 
-   Break down cost-per-unit by scale unit?
- How do I correlate activities across component boundaries (e.g. tracking a set of events from a certain device within a certain session)?
- How do I detect when a component (subsystem) fails? (e.g., a device sends a high priority event that the cloud gateway writes to a queue, but a failed consumer never processes it from the queue.)

## Testing
- How do I simulate the load from a million devices?
- How can I determine the bounds of a scale unit? That is, the reliable system load that a scale unit can support.
- How do I test a complex distributed system in order to ensure correct?
    - Should I simulate failures (e.g. fault injection)? If so, how?
