# Architecting an IoT solution

## The problem space 

We've identified some common scenarios and challenges in IoT. These include:  

**Managing devices**. This includes provisioning new devices, taking devices offline for maintenance or retirement, and device configuration.

Devices may need to be provisioned in the field (e.g., installing sensors in a building), using a self-serve model. That is, the technician who installs or services the device can provision it right there in the field, possibly using a mobile app. In that scenario, it may be important to protect the device from being reconfigured by hackers. 

**Ingesting data**. The most basic task of an IoT system is getting data into the back end, reliably and at scale. Considerations include:

- The expected data rate (how many devices; how many events per device). 
- Physical network requirements.
- Message protocols. (HTTP? [AMQP](https://www.amqp.org/)? Others?) 
- Protocol translation. In some cases, you might need to translate output from the devices into the expected ingestion format. For example, you might have legacy devices that use non-standard protocols.
- Authentication. Only legitimate devices should be able to send data.  
  
During development, it is common to use a **device simulator**, which feeds simulated data to the system. A simulator is also useful for load-testing the back end.

**Consuming data**. Once data is in the system, what are you doing with it? Here are some typical scenarios that we've identified:

- Short-term data aggregation: "In the last 5 minutes, what was the average temperature?"
- Critical alerts: "This unit is overheating!"
- Querying recent data: "In the past week, how many times did the temperature fall outside of a given range for more than ten minutes?"
- Long-term storage
	- Auditing 
	- History
- Visualization. Business reporting and charting, dashboards, etc.

**Command and control**. In some IoT scenarios, a device can receive outbound messages from the back end. This doesn't apply to every IoT solution.    

**Backend services**. Here are some things that you might build on the back end:

- Dashboards.
- System administration, to control who has access to which parts of the system. 
- APIs. You might create API layers so that consumers don't have to deal with raw data. APIs can also help protect privacy, and handle authorization.

**Cross-cutting concerns**. 

- **Security.** Security must be baked into the entire system. IoT security is a huge topic, and the industry is evolving quickly. (See [IoT Security: What are the Challenges?][security-challenges])
- **Multi-tenancy.** It is quite common for IoT solutions to have multiple users/customers. When a customer logs into their dashboard, they should see their devices only. 
- **Cloud readiness.** The solution must be reliable and scalable.

 
## Logical architecture 
 
The following logical architecture is derived from a large number of implementations that we have analyzed. It is intended to be a generic description of an IoT solution, which can be adapted to specific cases.

![IoT Reference Architecturee](media/reference-architecture.png)

In this diagram:

- The **remote site** is where the devices are physically located. In most IoT scenarios, there are many remote sites. Sometimes this is called _the field_, as in, "we have devices deployed in the field". In some scenarios, but certainly not all, the remote site is untrusted. That is, unauthorized people have physical access to the devices. For example, traffic cameras might be placed in a public area.  

- **Devices** are things that generate data. A device may be simple, like a single temperature sensor, or composite and include a variety of sensors. A device can communicate with the system running in the cloud, through one or more gateway services. Devices might also communicate directly with each other. The capabilities of a device can vary greatly, from highly constrained battery-powered micro-controllers to sophisticated always-on devices running full-fledged operating systems.

- Some IoT solutions incorporate a **field gateway**, which is a local network at the remote site. A field gateway relays data from the devices to the cloud (possibly aggregating it first), and can provide a distribution point for commands sent from the cloud, directing operations to the appropriate devices. The field gateway might also implement local logic, such as data analysis, protocal translation, message encryption, or handling events that require very low latency. 

- The **cloud gateway** is the primary endpoint for ingesting device data. It will likely have other functions, such as handling authentication and authorization at the application level, translation of protocols or encoding formats, connection management, and command delivery.

- The **device registry** is used to identify devices and store relevant metadata, such as the physical location of the device. The registry is also used for [device provisioning][device-provisioning]. (Provisioning is the act of providing a device with credentials so that it can authenticate with the backend.) Other components in the system might use the device registry as well.

- The **event stream** is a transient store of event data received by the cloud gateway. It differs somewhat from the other items in this diagram, because it is mainly an infrastructure concern. We are calling it out here because of its impact on the architecture. It serves at least two functions; load leveling and composability. A service reading data from the event stream is called a _consumer_. In our diagram, we identify three broad categories of consumer: analytics, storage, and general business logic. For an in-depth discussion of event streams, see [The Role of Event Processing][event-processing].

- **Analytics** in this diagram refers to *real-time analytics*, which is the analysis of event data as it is received, as opposed to batch analytics. It is typically used when the system needs to respond to something in a timely manner. Another common term is _hot path analysis_. Be aware that "real-time" in this context really means near-real-time; "real-time" can mean something very different in [other contexts][real-time-example]. 

- The event stream is not a permanent store, and is difficult to query directly. This create a need for a **storage** component. There are lots of different business reasons for storing event data, including batch analytics (e.g. Hadoop), ad-hoc querying, and auditing. We'll go into some of these scenarios in detail. The system might store the raw event data received by the cloud gateway, or it might store data that is derived from the event stream. There is also no assumption here about the type of storage (relational, NoSQL, etc.). The system might include several storage components for different purposes.

- **Command & Control** handles outbound messages to devices. Notice in our diagram that this component only communicates to the cloud gateway, rather than communicating directly with devices. This is a security consideration based on the [Service Assisted Communication][] pattern.

- **Business logic** is a bit of a catch-all. Here is an example: A user turns off a remote device. The request to turn off the device results in an event in the event stream. Some business logic recognizes the event, confirms that the user is authorized to perform the action, confirms that the device is in a state that allows the action, and sends a command to the device via the command and control component.
There are lots of ways to implement business logic, but one increasingly popular approach is the [actor model]. Learn more about Azure's forthcoming native support for actors with [Service Fabric][service-fabric-actors].

<todo task="update this statement after service fabric is released" />

### What's not on this diagram

There are a couple of concepts we omitted from this diagram for simplicity that should still be mentioned.

- _Not all event data needs to be streamed._ There are many situations where data generated by devices may not need to be streamed to the backend. In these situations, data might be uploaded as a batch either at regular intervals or as needed. This is more common in scenario where a field gateway is used. The batch data may or may not participate in the event stream.
- _Not all data is event data._ We are emphasizing event data in this guidance because it is common in these scenarios. However, there may be a need to regularly ingest data that cannot be define as event data. Again, this depends on your specific business requirements.
- _An API for managing devices._ Most systems will need a facility for provisioning,  deprovisioning, and general device management (such as updating metadata about a specific device). This facility will typically interact with the device registry.
 
  
## Strategies for success

Here are some ideas to keep in mind from the onset while designing an IoT solution.

### Start small, think big

It's easy to become overwhelmed by the sheer volume of data and the number of devices that an IoT solution might be expected to handle. It can also be hard to predict in advance which sensors and devices might provide the data that your system requires. 

Rather than try to build a system that solves every problem at once, start by end-to-end prototyping with a small number of devices. Your focus should be on designing an architecture that will scale, is low-latency, and can handle extreme hardware and software heterogeneity. Consider that some critical data might require immediate action, and that a large volume of data might be stored for later analysis and decision-making. 

Once you understand how data flows from the devices all the way through the system to the analytics and business logic, you are more likely to spot issues that you might otherwise miss. It is easier to address these issues at small scale. Be prepared to evaluate whether the prototype meets expectations, make any necessary adjustments, and then document the lessons learned so that you can apply them to the large scale.

### Focus on telemetry first

Devices and sensors are intended to report information about their state. Many devices can also accept commands to change their state. However, you should probably avoid adding complex state-changing logic into the system, until you fully understand the data coming in from the devices. So it's a good idea to focus first on telemetry. This includes not only the state information that a device reports, but also diagnostics that help to establish device health. 

Moreover, privacy and security issues tend to be simpler for telemetry than for command and control, so this approach lets you focus on the security and manageability of the system from the start.

Prototyping with telemetry can also give you an idea of how your system will respond under load. The system must be able to ingest data quickly, process it within the expected time-frame, and store the results without falling over.

### Don't block critical work

Critical state information from devices may need to be handled quickly. Be careful to avoid bottlenecks in the part of the system responsible for this kind of processing. For example, a service watching for critical events in the event stream should not also be responsible for processing non-critical events. This is an application of the [Separation of Concerns][separation-of-conerns] principle.

### Handle defense in depth

Consider security, identity, and management from the very start. Think about security on the device itself, as data is transferred over the network, and as it is received, stored, and processed. In particular, consider the following aspects:

- Physical security and tamper detection of all devices in the field.
- Firmware security and secure boot to prevent hardware being compromised.
- Network protocol security to protect data in flight.
- Application security to prevent loss and leakage of sensitive information.
- Identity management for devices (prevent unexpected, rogue devices from being introduced into the system) and users.
- Data privacy protection and controls to ensure that data is stored securely.

Security should be a top concern for any IoT solutions. See [Security Challenges][security-challenges] for additional high-level considerations.

### Design for a long life

An IoT solution is a significant investment. Once a device is deployed to the field, it might be there for a long time. Don't necessarily expect that you will can replace it with newer hardware or update the firmware easily. Consequently, an IoT system should be designed to last, possibly for decades. 

On the other hand, the pace of technological change means that newer, more powerful devices are always arriving to market, enabling more advanced scenarios as your IoT system evolves and expands. The system should be designed not just for today's devices, but should be flexible enough to integrate new sensors, new hardware, and new device capabilities.

### Design for change

Design the system with the understanding that there will be change. A monolithic system with rigid expectations about the shape of flow of data is cheaper and easy to build, but prohibitively expensive to extend and maintain. Designing system for change is the topic of entire books; we can only emphasize its value here.

- Separate concerns. This is the [same principle][separation-of-conerns] mentioned above. It's a fundamental concept.
- Design for failure. Failures can and will occur at all levels in system; from missing fields in data to missing services. See the [resiliency patterns][resiliency-patterns] in our _Cloud Design Patterns_.
- Enable composition. Break down tasks into discrete units that can be reused and combined using patterns like [Pipes & Filters][pipes-and-filters]. Consider patterns like [Event Sourcing][event-sourcing] that enable future unanticipated components to easy integrate into an existing system. 

It is also worth exploring the communities and materials about ideas like [Event-driven Architecture][event-driven-architecture] and [Microservices][microservices].

[actor model]: https://en.wikipedia.org/wiki/Actor_model
[device-provisioning]: 07-device-provisioning.md
[event-driven-architecture]: https://en.wikipedia.org/wiki/Event-driven_architecture
[event-processing]: 08-event-processing.md
[event-sourcing]: https://msdn.microsoft.com/en-us/library/dn589792.aspx
[microservices]: http://martinfowler.com/articles/microservices.html
[pipes-and-filters]: https://msdn.microsoft.com/en-us/library/dn568100.aspx
[real-time-example]: https://en.wikipedia.org/wiki/Real-time_Control_System
[resiliency-patterns]: https://msdn.microsoft.com/en-us/library/dn600215.aspx
[security-challenges]: 10-security-challenges.md
[separation-of-conerns]: https://en.wikipedia.org/wiki/Separation_of_concerns
[Service Assisted Communication]: http://blogs.msdn.com/b/clemensv/archive/2014/02/10/service-assisted-communication-for-connected-devices.aspx
[service-fabric-actors]: https://azure.microsoft.com/en-us/documentation/articles/service-fabric-reliable-actors-introduction/
