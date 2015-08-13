# A few quick tips

These are some ideas to keep in mind from the onset while designing an IoT solution.

## Start small, think big

It is very easy to become overwhelmed by the sheer volume of data and the number of devices that an IoT solution might be expected to handle. In addition, it can be hard to predict in advance which sensors and devices might provide the data that your system requires. 

Rather than attempting to build a system that solves every problem at once, start end-to-end prototyping with a small number of devices. Your focus should be on designing an architecture that will scale, is low-latency, and that can handle extreme hardware and software heterogeneity. Consider that some critical data might require immediate action while a large volume of information may need to be stored for subsequent analysis and decision-making. Furthermore, identifying how data flows from devices all the way through the system to the analytics processing and business domain logic can help to highlight issues that might otherwise be missed.

Implementing an initial subset of the system will help to highlight where possible issues are likely to occur. These issues include resolving device identity, management, update, and deployment of devices, and security concerns. It is easier to address these issues in the small scale. Be prepared to evaluate whether the prototype meets expectations, make any necessary adjustments, and then document the lessons learned so that you can apply them to the large scale.

## Focus on telemetry first

Devices and sensors are intended to report information about their state, while many devices can also accept commands to change their state controlled by the business logic of the system. It can be inefficient to try and incorporate complex state-changing business processes in an IoT system until the data reported by devices and device capabilities are fully understood. Instead, start by focusing on the telemetry provided by devices (not only the state information that a device reports, but also diagnostics that help to establish device health). Privacy and security issues tend to be simpler for telemetry than for command and control, so this approach enables you to focus on security and manageability of the system before becoming embroiled in detailed transformational logic.

Prototyping with telemetry can also give you an idea of how your system will respond under load. The system must be capable of ingesting data quickly, processing it within the expected time-frame, and storing the results without becoming overwhelmed.

## Don't block critical work

Critical state information retrieved from devices may need to be handled quickly. Be careful to avoid bottlenecks in the part of the system responsible for this kind of processing. For example, a service watching for critical events in the event stream should not also be responsible for processing non-critical events. This is an application of the [Separation of Concerns][separation-of-conerns] principle.

## Handle defense in depth

Consider security, identity, and management from the very start. Think about security on the device itself, as data is transferred over the network, and as it is received, stored, and processed. In particular, consider the following aspects:

- Physical security and tamper detection of all devices in the field.

- Firmware security and secure boot to prevent hardware being compromised.

- Network protocol security to protect data in flight.

- Application security to prevent loss and leakage of sensitive information.

- Identity management for devices (prevent unexpected, rogue devices from being introduced into the system) and users.

- Data privacy protection and controls to ensure that data is stored securely.

Security should be a top concern for any IoT solutions. See [Security Challenges][security-challenges] for additional high-level considerations.

## Design for a long life

An IoT solution is a significant investment. Once a device has been deployed to the field it might be there for a long time; don't necessarily expect that you will be able to replace it with newer hardware or update the firmware easily. Consequently, an IoT system should be designed to last, possibly for decades. 

You should also consider that the nature of technology is such that newer, more powerful devices can become available at relatively short notice. Innovations that increase the functionality or speed of devices can enable more advanced scenarios as the system evolves and expands. An IoT system should therefore be designed to support not only the current state of the art devices, but also allow for the integration of sensors, hardware, and capabilities (including new software and infrastructure) that may become available in the future.

## Design for change

Design the system with the understanding that there will be change. A monolithic system with rigid expectations about the shape of flow of data is cheaper and easy to build, but prohibitively expensive to extend and maintain. Designing system for change is the topic of entire books; we can only emphasize its value here.
- Separate concerns. This is the [same principle][separation-of-conerns] mentioned above. It's a fundamental concept.
- Design for failure. Failures can and will occur at all levels in system; from missing fields in data to missing services. See the [resiliency patterns][resiliency-patterns] in our _Cloud Design Patterns_.
- Enable composition. Break down tasks into discrete units that can be reused and combined using patterns like [Pipes & Filters][pipes-and-filters]. Consider patterns like [Event Sourcing][event-sourcing] that enable future unanticipated components to easy integrate into an existing system. 

It is also worth exploring the communities and materials about ideas like [Event-driven Architecture][event-driven-architecture] and [Microservices][microservices].

[separation-of-conerns]: https://en.wikipedia.org/wiki/Separation_of_concerns
[security-challenges]: TODO
[resiliency-patterns]: https://msdn.microsoft.com/en-us/library/dn600215.aspx
[pipes-and-filters]: https://msdn.microsoft.com/en-us/library/dn568100.aspx
[event-sourcing]: https://msdn.microsoft.com/en-us/library/dn589792.aspx
[event-driven-architecture]: https://en.wikipedia.org/wiki/Event-driven_architecture
[microservices]: http://martinfowler.com/articles/microservices.html