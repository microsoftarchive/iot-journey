# The Role of Event Processing

Building an IoT solution does not mandate the use of event streams or event processing. In the same way that a traditional line-of-business application does not necessitate a three-tier architecture. However, event processing is so commonly associated with IoT, and plays such a central role in the typical architecture, that it warrants a discussion.

## What is Event Processing?

First, what do we mean by _event_? In the most strict usage, an event is something that happens independent of the system that a part of the system can identify and record. For example, in the context of our [Fabrikam scenario][fabrikam-scenario], let's say that the temperature in a particular building  rises above 80°F/27°C. A device in the building with the appropriate sensor  detect the increase in temperature and sends data to the backend. The actual change in temperature is considered to be the _event_ and the data that the device produces is the _event data_ or _event notification_. However, it is common to refer to the event data simply as the event. So don't let this distinction confuse you.

The processing of the event data as it is received is frequently referred to as [Event Stream Processing][event-stream-processing] or _ESP_.  This is in contrast to data that is first collected and then processed at a later time as a batch. You will often hear terms such as _real-time_ and _near real-time_ used to describe processing the data as it is received. (Though we should again emphasize that real-time can have a different meaning in other contexts.)

What do we mean by _processing_? This is an inherently vague term, however some common activities associated with processing include:

- _Transformation_. As event data is received it may need to be filtered and projected before being consumed by other parts of the system. This includes a wide range of activities in itself. Transformation might include debouncing, normalization, annotation, or numerous other activities. 

	Here's a simplistic example, suppose we are collecting temperature readings from lots of different devices from different manufacturers. Some record the readings in Fahrenheit and others in Celsius. We might choose to project all of the readings to Celsius. 
	
- _Aggregation_. The raw unprocessed stream of event data might be too granular for some consumers of the data. In these cases, the event data might be aggregated over a window of time in order to produce a more coarse event stream. 

	Perhaps the total number of temperature readings per building is approximately 1,000 events per second. If we have 10 buildings, then the total ingress is approximately 10,000 events per second. This may be more that the backing storage technology for our executive dashboard can handle. We decided to take the average for each building over a 1 minute window. Then the stream of data going into storage is only about 170 events per second. 
	
- _Alerting._ One of the most useful types of processing is the simple analysis of event data to support timely responses. 

	A very simple example might be monitoring the event data for temperature readings exceeding 400°F/200°C. If such a value is detected in the event stream, then the system could raise an appropriate notification.
	
- _Pattern analysis._ Another very powerful technique is the act of inferring or deriving new events from the existing events. The existing events may even be from disparate sources. 
	
	Following our Fabrikam scenario still, we might have several devices reporting unexpected levels of humidity. Some of the devices are the second floor and some are on the first floor, but all of them are located in the northwest corner of the building. From these events the system can infer that there is likely a water present on the second floor in the northwest corner.

These examples are not meant to be a formal classification of event processing, nor are they necessarily comprehensive. They are intended to demonstrate the breadth of what is meant by event processing.

## Event Streams vs Message Queues

Event stream, at least we are treating them in this guidance, have similarities with message queues. Those familiar with patterns such as [enterprise service bus][service-bus-pattern] or classic works such as Hohpe and Woolf's [Enterprise Integration Patterns][], may be quick to superimpose messaging patterns onto event streams. While these patterns are still relevant for IoT solutions, they are distinct in many ways from those associated with event streams.

- Messages in a queue have an inherent state related to whether or not they have been processed. This state is a characteristic of the queue itself. Often messages are removed from a queue after the have been processed.  In contrast, event data in an event stream does not have a state. Consumers of an event stream (and there may be many consumers) maintain their own cursor. Consequently, an individual consumer of an event stream may even "replay" a portion of the stream that it has already processed.

- Message queues support the idea of multiple consumers using a [publish and subscribe][publish-subscribe-pattern] model. Consumers must register with the message queue in order to subscribe. The queue in turn has knowledge of subscribers. A consumer cannot process messages that occurred prior to its subscription. In contrast, an event stream has no knowledge of its consumers. Consumers do not need to register their interest and consequently they can read any event data in the event stream.

Two benefits that both event streams and message queues provide are load leveling and encapsulation.

 - _Load leveling_. If consumers of events or messages had to process them as quickly as they were produced, they would likely be overwhelm by surging in the traffic. A sudden increase in the number of events or messages would could cause a failure in the consumer resulting in data loss. Both event streams and message queues acts as buffers in a system. They allow consumer to work at their own pace without being overwhelmed.
 
 -  _Encapsulation._ The parts of the system producing event data should have no knowledge of how the event data is consumed and processed. This may seem like an obvious concept, but it is surprisingly easy to overlook. In addition to the separation between event producers and event consumers, there should also be a separation between the individual event consumers. Both message queues and event stream can act as a point of composition in your architecture.
 
With respect to Microsoft Azure and its platform-as-a-service offerings, [Service Bus][azure-service-bus] provides message queues and [Event Hubs][azure-event-hubs] provides event streams.

## Event Streams on Azure

> TODO Is this sectional useful? Should it be part of _Event Ingestion_ instead?

If you choose to use an event stream in your solution, there two technologies that you are likely to consider: [Apache Kafka][apache-kafka] and [Azure Event Hubs][azure-event-hubs].

Functionally the two are very similar. However, Event Hubs is offered as a managed service. Whereas if you chose to use Kafka, you would need to provision the necessary virtual machines and manage your own cluster.

The primary motivation for choosing to run Kafka would be if you had an existing dependency in a system that you were migrating to Azure.

One notable feature available in the .NET SDK for Event Hubs is the [event processor host][event-processor-host]. It's a high level API for consuming events that simplifies keeping track of which events in which partition have been read.

## Event Consumption Patterns

> TODO This content may also need to be relocated to another section

These are not meant to be a formal taxonomy of patterns for consuming event data, rather they are some common practices that we have observed while working with Event Hubs. 

There are a few assumptions common across all of these patterns:
- We typically receive a batch of event data at a time, rather than single event data. 
- We receive events within the context of an Event Hub _partition_.
- A consumer of events has a single purpose. This aligns with the idea of logical consumers with independent views of the event stream. Note that both Event Hubs and Kafka refer to these as _consumer groups_.

For more background on concepts like partitions and consumer groups, we recommend reading this [overview of Event Hubs][event-hub-overview].

We will also use the term _checkpoint_ to describe the act of an event consumer recording the most recent point in the event stream that it has successfully processed.

### Serial Processing

The most simple consumption pattern is to serially process each event data. This means that when a batch of event data is received, we iterate over each one, successfully processing it before moving on to the next event data. You would also likely checkpoint after each successfully processed event data. All of the event data in the batch would be processed before the next batch is received.

This approach is most attractive when the order of processing is significant.

This pattern has the worst performance characteristics. It has the most overhead for checkpointing, due to the frequency of checkpointing. It also compounds delays in processing more quickly due to its serial nature.

### Parallel Processing

All of the event data in a batch can be processed concurrently instead of serially. You would only checkpoint after the entire batch was successfully processed. The entire batch would be processed before the next batch is received.

This approach is attractive when the order of processing is not significant. Contention over system resources inhibits the benefits of parallelization.

Depending on your platform and system resources, you may want to bound the concurrency. For example, if your processing is CPU-bound consider limiting the concurrency to the number of available cores.

While this pattern is likely to perform better than serial processing, it introduces complications when checkpointing. See the _high watermark problem_ below.

### Buffering and Batching

In this pattern, a batch of event data is buffered in memory when received. Multiple batches may be received and buffered. At some point, a certain threshold is reached and processing of everything in the buffer is triggered. 
The trigger might be based on the number of event data, the total size in bytes of the buffered data, or a timeout.

This is a useful technique when data needs to be written to some store; since writing a batch of records in a single operation is frequently more efficient than writing them individually.

Processing the buffered data is typically an atomic operation; especially if the processing in question is writing the data to storage as a batch. However, this means that a failure during processing affects all of the event data that participated in the batch operation. 

Allowing for partial processing of the buffered data might make sense in others scenarios, but it complicates the logic needed to checkpoint. See the _high watermark problem_ below.

### Dispatching

Sometimes there is a need to examine each event data and process it differently depending on its characteristics. The characteristics used to classify event data can vary from schema to serialization format to the presence or value of specific fields. Generally, some sort promoted property is used for classification in order to avoid the cost of deserialization. For example, an event data might have property called `SchemaType` that identifies how the body of the event data should be interpreted. 

This classification is then used to look up some method of processing. This processing method might be a function invoked by the consumer itself. Alternatively, the method might be to place the event data in some other event stream or message queue that has its own set of consumers.

This pattern breaks our assumption about single-purpose event consumers. However, there are sometime strong technical reasons for using this pattern. For example, there are limits on the number of consumer groups supported by an Event Hub. If the functionality required by your system exceeds this limit, you should consider this approach.

This pattern can also be combined with _Serial Processing_ and _Parallel Processing_.

## Problems with Consumption

There are a number of common problems you will encounter when consuming events.

You do not want delays in the processing to accumulate. Problems that cause the processing to take longer that expected, especially in services external to the consumer, can quickly compound. For this reason, you should consider having reasonable timeouts on all processing operations.



### Checkpoint and The High Watermark Problem
	
	What happens when you process an event twice?
	What do failures really mean?
	Delays in processing affect subsequent work.

>	If a single event data takes a long time to process, then subsequent event data are delayed. This can be mitigated by incorporating a timeout mechanism into the processing logic.


## Related Topics and Resources

- **Complex Event Processing** or CEP is a more advanced topic associated with event stream processing. It deals with relationships between simpler events. For a brief overview, start with the [wikipedia article][complex-event-processing]. For a more thorough discussion, see [The Power of Events][power-of-events] by David Luckham.

- The book, [**I ♥ Logs**][i-heart-logs] by Jay Kreps, is an excellent introduction to a "log-centric" architecture and how it helped LinkedIn overcome problems of complexity and scale.

- The documentation for Apache Kafka has a great overview about the [motivations behind its design][kafka-design].

- When searching for information on event processing, you will likely encounter [event-driven architecture][event-driven-architecture]. There are many useful ideas in this pattern, but it tends to focus on message queues and not event streams.

- Another popular pattern you'll encounter is [Event Sourcing][fowler-event-sourcing]. However this pattern is less about infrastructure and more about domain modeling. We also discuss this pattern in our [Cloud Design Patterns][pnp-event-sourcing].

- Finally, there are a lot of interesting ideas about events being discussed in the [functional reactive programming][functional-reactive-programming] community.

[fabrikam-scenario]: TODO
[event-hub-overview]: https://azure.microsoft.com/en-us/documentation/articles/event-hubs-overview/
[event-processor-host]: https://azure.microsoft.com/en-us/documentation/articles/event-hubs-programming-guide/#event-processor-host
[apache-kafka]: https://kafka.apache.org/
[azure-service-bus]: http://azure.microsoft.com/en-us/documentation/services/service-bus/
[azure-event-hubs]: http://azure.microsoft.com/en-us/services/event-hubs/
[event-stream-processing]: https://en.wikipedia.org/wiki/Event_stream_processing
[complex-event-processing]: https://en.wikipedia.org/wiki/Complex_event_processing
[event-driven-architecture]:https://en.wikipedia.org/wiki/Event-driven_architecture
[service-bus-pattern]: https://en.wikipedia.org/wiki/Enterprise_service_bus
[Enterprise Integration Patterns]: http://www.enterpriseintegrationpatterns.com/books1.html
[publish-subscribe-pattern]: https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern
[power-of-events]: http://www.complexevents.com/books/
[i-heart-logs]: http://shop.oreilly.com/product/0636920034339.do
[kafka-design]: https://kafka.apache.org/documentation.html#design
[fowler-event-sourcing]: http://www.martinfowler.com/eaaDev/EventSourcing.html
[pnp-event-sourcing]: https://msdn.microsoft.com/en-us/library/dn589792.aspx
[functional-reactive-programming]: https://en.wikipedia.org/wiki/Functional_reactive_programming