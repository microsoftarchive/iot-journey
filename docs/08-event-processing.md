# The Role of Event Processing

This topic provides some general guidance on processing event streams. Not every IoT solution requires an event stream architecture, just as not every line-of-business app needs a three-tier architecture. However, event processing is a very common and useful approach in IoT. 

## What is event processing?

First, what do we mean by _event_? In the most strict definition, an event is something that happens outside of the system, which a component in the system can identify and record. For example, in the context of our [Fabrikam 
scenario][fabrikam-scenario], suppose the temperature in a building rises above 80°F (27°C). A sensor detects the change and sends data to the backend. The actual change in temperature is the _event_, and the data
 sent by the device is the _event data_ or _event notification_. However, it's common to use the word _event_ to mean the event data.

One way to handle event data is simply to collect it first, and process it later. In contrast, [Event Stream Processing][event-stream-processing] means processing the event data as it's received. This is sometimes called _real-time_ or _near real-time_ processing, although the phrase "real-time" can mean something very different in [other contexts][rtc]. 

What are some ways that an IoT solution might process event data? Here are some examples. This is not a formal taxonomy or even a comprehensive list, but it shows some of the scope of what's possible.

- _Transformation_. Event data may need to be filtered and projected before other parts of the system can consume it. Transformations might include debouncing, normalization, or annotation. For example, suppose our IoT solution includes a mix or sensors that send data in both Fahrenheit and Celsius. The solution might project (convert) all of the readings to Celsius. 
	
- _Aggregation_. The raw event stream might be too granular for some consumers. In that case, you might aggregate the event data over a window of time, to produce a more coarse-grained event stream. For example, suppose there are 10 buildings, and each generates 1,000 events per second. This may be more than the backing storage for a real-time dashboard can handle. Therefore, you might take the average for each building over a 1-minute window, thereby reducing the data stream going into storage to about 170 events per second. 
	
- _Alerting._ One of the most useful types of processing is analysis of event data to support timely responses. For example, if a temperature reading exceeds some threshold, the system could raise an alert (send a text, etc).
	
- _Pattern analysis._ Another powerful technique is to look for patterns in the event stream and use these to derive new events. For example, suppose several devices report unexpected levels of humidity. Some of the devices are on the second floor, and some are on the first floor, but all are located in the northwest corner of the building. From these events, the system infers there is likely water on the second floor in the northwest corner. This inference triggers a new event. Pattern analysis might take input from several disparate event streams. 


## Event streams versus message queues

Event streams, as we've been treating them in this guidance, are similar in some ways to message queues. If you're already familiar with patterns such as [enterprise service bus][service-bus-pattern] or classic works such as Hohpe and Woolf's [Enterprise Integration Patterns][], you may be tempted to impose messaging patterns onto event streams. While those patterns are still relevant for IoT solutions, they are distinct in many ways from event streams.

- Messages in a queue have an inherent state, related to whether or not they have been processed. This state is a characteristic of the queue itself. Often messages are removed from a queue after they have been processed. In contrast, an event stream does not maintain any state about event consumption. Consumers of the data maintain their own cursor. An individual consumer may even replay a portion of the stream that it has already processed.

- Message queues can support multiple consumers using a [publish and subscribe][publish-subscribe-pattern] model. Consumers must register with the message queue in order to subscribe. The queue in turn has knowledge of subscribers. A consumer cannot process messages that occurred prior to its subscription. In contrast, an event stream has no knowledge of its consumers. Consumers do not need to register with the stream, and can read from anywhere in the event stream.

Two benefits that both event streams and message queues provide are load leveling and encapsulation.

 - _Load leveling_. Event streams and message queues both act as buffers, so that consumers don't need to process events/messages at the exact rate they are produced. Otherwise, a temporary surge in the rate of events/messages might overwhelm the consumer and cause it to fail, resulting in data loss. With a stream or queue, the system can buffer data during surges, allowing the consumer to catch up.
 
 -  _Encapsulation._ The parts of the system producing event data should have no knowledge of how the data is consumed and processed. This may seem like an obvious concept, but it is surprisingly easy to overlook. In addition to the separation between producers and consumers, there should also be a separation between the individual consumers. Both message queues and event streams can act as a point of composition in your architecture.
 
With respect to Microsoft Azure, [Service Bus][azure-service-bus] provides message queues and [Event Hubs][azure-event-hubs] provides event streams. Our reference implementation uses Event Hubs. (See [Event Ingestion][event-ingestion].)

## Event consumption patterns

Here are some common practices that we have observed while working with Event Hubs. These are not meant to be a formal taxonomy of patterns for consuming event data.

We make the following assumptions:

- The consumer typically reads a batch of event data at a time, rather than a single event. 
- We receive events within the context of an event hub _partition_.
- An event consumer has a single purpose. This aligns with the idea of logical consumers with independent views of the event stream. Note that both Event Hubs and Kafka refer to these as _consumer groups_.
- An event consumer must record the most recent point in the event stream that it has successfully processed. This process is called _checkpointing_. Checkpointing enables a new consumer instance to pick up where another instance left off.

For more background on concepts like partitions and consumer groups, we recommend reading this [overview of Event Hubs][event-hub-overview].

### Serial processing

The simplest approach is to process each event before moving to the next. When the consumer receives a batch of event data, it iterates over each event, completing the processing before moving to the next. In this approach, you would likely checkpoint after each successfully processed event. All of the event data in the batch is processed before the next batch is received. 

This approach is most attractive when the order or processing matters. However, it has the worst performance characteristics. It requires the most overhead for checkpointing, due to the frequency of checkpointing. It also compounds delays in processing more quickly, due to its serial nature.

### Parallel processing

In this approach, the comsumer processes all the events in a batch concurrently, checkpointing after the entire batch is successfully processed. All of the event data in the batch is processed before the next batch is received.

This approach is attractive when the order of processing is not significant. 

Contention over system resources can limit the benefits of parallelization. Depending on your platform and system resources, you may want to bound the concurrency. For example, if your processing is CPU-bound, consider limiting the concurrency to the number of available cores.

While this pattern is likely to perform better than serial processing, it introduces complications when checkpointing. Just because event N was successfully processed, does not mean that event N-1 was processed. If you checkpoint at the _latest_ successfully processed event, you might miss processing an event earlier in the stream. If you checkpoint at the _earliest_ successfully processed event, you may end up processing an event twice. (You can ameliorate the latter problem by designing your event processing logic to be idempotent.) 

### Buffering and batching

In this approach, when the consumer receives a batch of event data, it buffers the data in memory. Multiple batches may be received and buffered. When a certain threshold is reached, the consumer processes everything in the buffer. The threshold might be the number of events, the total size of the buffered data, or a timeout.

This technique is useful when data needs to be written to a store, because writing a batch of records in a single operation is often more efficient than writing them individually.

Processing the buffered data is typically an atomic operation, especially when you are writing the data to storage. However, this means that a failure affects all of the event data in the batch operation. In some scenarios, you might allow for partial processing of the buffered data, but that complicates the checkpoint logic, similar to parallel processing.

### Dispatching

Sometimes you need to examine each event and process it differently, depending on some classification, such as the event schema, the serialization format, or the presence or value of specific fields. Generally, you would want to key off of a property in the event metadata, so that you don't need to deserialize the event data. For example, an event might have a property called `SchemaType` that identifies how the body of the event data should be interpreted. This classification is then used to look up some method of processing. The processing method might be a function invoked by the consumer itself, or the consumer might put the event data into another event stream or message queue, with its own consumers.

This pattern breaks our assumption about single-purpose event consumers. However, there are sometimes strong technical reasons for using this pattern. For example, there are limits on the number of consumer groups supported by an event hub. If the functionality required by your system exceeds this limit, you should consider this approach.

This pattern can also be combined with _Serial Processing_ and _Parallel Processing_.

## Commong problems during event consumption

Here are some common problems you may encounter.

**Delays.** You don't want delays in processing to accumulate. Problems that cause processing to take longer than expected, especially in services external to the consumer, can quickly compound. For this reason, consider having reasonable timeouts on all processing operations.

**Failures**. Sometimes processing will fail, either for an individual event or for a batch. This may be due to a transient error, such as an external service being temporarily unavailable, or to errors in the event data itself. Exactly how failures are treated is specific to your business. However, be aware that errors and error handling can cause processing delays. 

**Retries**. Failures often result in retries. Therefore, processing an event should be an idempotent operation.

**Load shedding.** A consumer might end up in a situation where it has more data than it can process. This might happen because event data is being ingested faster than the consumer can process it. In that case, the consumer needs to be scaled out, if possible. However, a consumer might fail to keep up because of unexpected processing failures (which in turn cause timeouts, retries, etc). In that situation, you should consider _load shedding_; that is, discarding some of the event data.

If you need to discard data, the easiest option is to reject new data until all of the existing data is processed. However, there are many situations where newer data is more relevant than older data. 

Ultimately, the best way to avoid data loss is by testing and monitoring your system.

## Related Topics and Resources

- **Complex Event Processing** or CEP is a more advanced topic associated with event stream processing. It deals with relationships between simpler events. For a brief overview, start with the [wikipedia article][complex-event-processing]. For a more thorough discussion, see [The Power of Events][power-of-events] by David Luckham.

- The book, [**I ♥ Logs**][i-heart-logs] by Jay Kreps, is an excellent introduction to a "log-centric" architecture and how it helped LinkedIn overcome problems of complexity and scale.

- The documentation for Apache Kafka has a great overview about the [motivations behind its design][kafka-design].

- When searching for information on event processing, you will likely encounter [event-driven architecture][event-driven-architecture]. There are many useful ideas in this pattern, but it tends to focus on message queues and not event streams.

- Another popular pattern you'll encounter is [Event Sourcing][fowler-event-sourcing]. However this pattern is less about infrastructure and more about domain modeling. We also discuss this pattern in our [Cloud Design Patterns][pnp-event-sourcing].

- Finally, there are a lot of interesting ideas about events being discussed in the [functional reactive programming][functional-reactive-programming] community.

[fabrikam-scenario]: 03-reference-implementation.md
[event-ingestion]: 04-event-ingestion.md
[event-hub-overview]: https://azure.microsoft.com/en-us/documentation/articles/event-hubs-overview/
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
[rtc]: https://en.wikipedia.org/wiki/Real-time_Control_System
