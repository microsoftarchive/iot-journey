# Build to the reference architecture

IoT is not specific to any particular sector or industry. Analysis has shown that the same principles can be applied regardless of whether an IoT solution is concerned with industrial automation and manufacturing, vehicle fleet management, healthcare, smart buildings, or a plethora of other cases in which remote devices provide information and can be controlled by some form of feedback loop. 

The following logical architecture is intended to be a generic description of an IoT solution which can be applied and adapted to any specific case.

![IoT Reference Architecture](media/what-is-an-IoT-solution/reference-architecture.png)

In this diagram:

- Devices are *things* (as in *Internet of Things*) that generate events. Devices may be simple or composite and include a variety of sensors. A device can communicate with the system running in the cloud, through one or more gateway services. Devices might also be able to communicate directly with each other.

- The Field Gateway can implement local logic close to a collection of devices. It is optional. The field gateway might act as an aggregator, accumulating responses from devices and combining them into events. It can also provide a distribution point for commands and other data sent from the system in the cloud, directing operations to the appropriate devices.

- The Cloud Gateway is the primary endpoint for ingesting events from devices in the field. It might also have other functions, such as handling authentication and authorization at the application level, translation, connection management, and command delivery.

- The Device Registry is used by the system to identify devices, and is maintained when devices are provisioned and deprovisioned. Some features of the cloud gateway (such as authentication) might need information that is held here.

- Event Stream Logic handles the stream of event information that arrives from devices in the field. It may compromise multiple consumers and is a point of composition in the system.

- Storage is the repository used by the system for holding event information and device status. This repository might compromise several data stores optimized for particular patterns of access and partitioned for scalability and performance.

- Analytics includes the hot path and the cold path. The hot path must be optimized to ensure that events are captured, processed, and reported within the required timeframe.

- Business Logic is the domain-specific logic of the system.

- Command & Control Logic handles outbound messages being sent to devices. This area is a considerable security concern - the outbound path must not be compromised as it could have serious consequences (consider what might happen if a device receives a rogue command).

- Data Visualization covers the presentation logic of the system, including business reporting and charting, as well as dashboards that can be used to assess the overall health and stability of the system.
