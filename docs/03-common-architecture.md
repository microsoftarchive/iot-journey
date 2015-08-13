# The common architecture for IoT solutions

IoT is not specific to any particular sector or industry. The same principles can be applied regardless of whether an IoT solution is concerned with industrial automation and manufacturing, vehicle fleet management, health care, smart buildings, or a plethora of other cases in which remote devices provide information and can be controlled by some form of feedback loop. 

The following logical architecture is derived from a large number of implementations that we have analyzed. It is intended to be a generic description of an IoT solution which can be applied and adapted to many specific cases.

![IoT Reference Architecturee](media/reference-architecture.png)

In this diagram:

- _Remote Site_ represents a location where devices are physically present. In most IoT scenarios there are many remote sites. The set of remote sites is sometimes referred to as _the field_. As in, "we have devices deployed in the field". In some scenarios, but certainly not all, the remote site is untrusted. That is, unknown agents could have physical access to devices at a remote site. For example, devices located in a public area such as a traffic camera.  

- _Devices_ are *things* (as in *Internet of Things*) that generate data; frequently in the form of event notifications. Devices may be simple or composite and include a variety of sensors. A device can communicate with the system running in the cloud, through one or more gateway services. Devices might also be able to communicate directly with each other. The capabilities of a device can vary greatly from highly constrained battery-powered micro-controllers to sophisticated always-on devices running full-fledged operating systems.

- _Field Gateways_ are located at remote sites with devices. They can implement local logic close to a collection of devices. They are not needed for all solutions. A field gateway might act as an aggregator, accumulating data from devices. It can also provide a distribution point for commands and other data sent from the system in the cloud, directing operations to the appropriate devices. It might perform analysis of local data as well as handling events where low latency and response time are critical.
In some senses, field gateways are just a more sophisticated device.

- The _Cloud Gateway_ is the primary endpoint for ingesting event data from devices in the field. It will likely have other functions, such as handling authentication and authorization at the application level, translation of protocols or encoding formats, connection management, and command delivery.

- The _Device Registry_ is used by the system to identify devices and store relevant metadata (such as, where a device is physically located). The registry also participates in the device provisioning story. Provisioning is the act of providing a device with credentials so that it can authenticate with the backend. Though not emphasized in this diagram, the data stored in the registry might be used by the Cloud Gateway or other components in the system.

- The _Event Stream_ is a transient store of event data received by the cloud gateway. It differs somewhat from the other items in this diagram in that it is primarily an infrastructure concern. We are calling it out here because of its impact on the architecture. It serves at least two functions; load leveling and composability. A service reading data from the event stream is called a _consumer_. In our diagram we identify three broad categories of consumers: analytics, storage, and general business logic. We'll talk in-depth about event streams when we discuss [Event Stream Processing][].

- _Analytics_ in this diagram refers to _real-time_ analytics. By real-time we mean performing an analysis on event data as it is received. Be aware that the term "real-time" can mean something very different in [other contexts][real-time-example]. This type of analysis is used when the systems needs to respond to something in a timely manner. The definition of "timely manner" depends on the business context. It is sometimes described as _hot path analysis_. There are other types of analytics that we have omitted from this diagram for simplicity.

- The event stream is not a permanent store for event data. Likewise, it is difficult to query directly. This presents a need for a downstream _Storage_ component (or components) in the system. There are lots of different business reasons for storing event data including from batch analytics (e.g. Hadoop), support for ad-hoc querying, and auditing. We'll go into some of these scenarios in detail. The actual data being stored might be the raw event data received by the cloud gateway, but it might also be data derived from the event stream. There is also no assumption here about the type of storage (relational, NoSQL, etc.). The same data may be persisted in different stores for different uses.

- _Command & Control_ handles outbound messages being sent to devices. Notice in our diagram that this component only communicates to the cloud gateway rather than communicating directly with devices. This is a security consideration based on the [Service Assisted Communication][] pattern.

- _Business Logic_ is a bit of a catch-all in this diagram. It is an area that differs the most depending on business scenarios. An example of business logic might be a user turning off a remote device. The request to turn off the device results in an event in the event stream. Some business logic recognizing the event, confirms that the user is authorized to perform the action, confirms that the device is in a state that allows action, and sends a message to the device via the command and control component.
There are lots of ways to implement business logic, but one increasingly popular approach is the [actor model]. Learn more about Azure's forthcoming native support for actors with [Service Fabric][service-fabric-actors].

<todo task="update this statement after service fabric is released" />

## What's not on this diagram

There are a couple of concepts we omitted from this diagram for simplicity that should still be mentioned.

- _Not all event data needs to be streamed._ There are many situations where data generated by devices may not need to be streamed to the backend. In these situations, data might be uploaded as a batch either at regular intervals or as needed. This is more common in scenario where a field gateway is used. The batch data may or may not participate in the event stream.
- _Not all data is event data._ We are emphasizing event data in this guidance because it is common in these scenarios. However, there may be a need to regularly ingest data that cannot be define as event data. Again, this depends on your specific business requirements.
- _An API for managing devices._ Most systems will need a facility for provisioning,  deprovisioning, and general device management (such as updating metadata about a specific device). This facility will almost certain interact with the device registry.

[Event Stream Processing]: TODO
[real-time-example]: https://en.wikipedia.org/wiki/Real-time_Control_System
[Service Assisted Communication]: http://blogs.msdn.com/b/clemensv/archive/2014/02/10/service-assisted-communication-for-connected-devices.aspx
[actor model]: https://en.wikipedia.org/wiki/Actor_model
[service-fabric-actors]: https://azure.microsoft.com/en-us/documentation/articles/service-fabric-reliable-actors-introduction/