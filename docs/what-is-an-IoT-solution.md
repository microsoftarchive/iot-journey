# What is an IoT solution?

The fundamental reason for implementing a network is to connect computing resources together. Initially local area networks were commonly used for many business scenarios such as sharing access to devices such as hard disks and printers, and then for communications applications such as email and messaging. The Internet enabled publicly accessible wide-area networking, allowing remote machinery and users to communicate. Nowadays, it is possible to connect almost any network-enabled device to the Internet to send and receive information from other similarly connected devices. This connectivity enables a wide range of scenarios, from remote monitoring and control of the heating and air conditioning of an apartments from a users smart phone, to highly complex situations such as driverless cars or remote controlled aircraft. This is the "Internet of Things" or IoT.

The concept of IoT is not new - the Internet has been around in various guises since the 1960s, and businesses have been connecting computers to hardware over a network for just as long. What is new is the affordability of hardware, the increased bandwidth and ubiquity of the Internet, the notion of virtualization, and the application of cloud services to combine devices together to support increasingly complex and large-scale scenarios. It is not uncommon for businesses to consider building systems that gather input from, and control, many thousands or even millions of heterogeneous items of equipment.

A development team faced with implementing a IoT solution has to wrestle with several significant challenges, including:

- What data is required to support the functionality of the solution, and what devices can provide this data?

- How can the system support a variety of different types of devices?

- How can the system control devices quickly and reliably?

- How will the system can scale and remain responsive as the volume of devices increases?

- Where will device data be stored, and for how long? How will this data be processed?

- How can a large number of devices be deployed and maintained in a cost-effective manner?

- How can network communications and data be protected from intrusion, interruption, or malicious external activities?

The following sections provide some high-level guidelines to help you address these challenges.

# Start small, think big

It is very easy to become overwhelmed by the sheer volume of data and the number of devices that an IoT system might be expected to handle. In addition, it can be hard to predict in advance which sensors and devices might provide the data that your system requires. 

Rather than attempting to build a system that solves every problem at once, start prototyping with a small number of devices but focus on designing an architecture that will scale, is low-latency, and that can handle extreme hardware and software heterogeneity. Consider that some critical data might require immediate action (the *hot path*) while a large volume of information may need to be stored for subsequent analysis and decision-making (the *cold path*). 

Implementing an initial subset of the system will help to highlight where possible issues are likely to occur. These issues include resolving device identity, management, update, and deployment of devices, and security concerns. It is easier to address these issues in the small scale. Be prepared to evaluate whether the prototype meets expectations, make any necessary adjustments, and then document the lessons learned so that you can apply them to the large scale.

# Focus on telemetry first

Devices and sensors are intended to report information about their state, while many devices can also accept commands to change their state. It can be inefficient to try and incorporate logic that implements business transformations in an IoT system until the data reported by devices and device capabilities are fully understood. Instead, start by focussing on the telemetry provided by devices (not only the state information that a device reports, but also diagnostics that help to establish device health). Privacy and security issues tend to be simpler for telemetry than for command and control, so this approach enables you to focus on security and manageability of the system before becoming embroiled in detailed transformational logic.

Prototyping with telemetry can also give you an idea of how your system will respond under load. The system must be capable of ingesting data quickly, performing hot path processing of this data within the expected time-frame, and store potentially large volumes of data for cold path analysis without becoming overwhelmed.

# Don't interrupt the hot path

Critical state information retrieved from devices may need to be handled quickly. Don't create processing bottlenecks in the hot path. For example, don't transform and manipulate the raw data unnecessarily unless the system can handle such transformations at scale. Additionally, store data for future analysis in its raw form and use the cold path processing to perform any required data conversions.

# Handle defense in depth

Consider security, identity, and management from the very start; this is not something that can be added in later. Think about security on the device itself, as it is transferred over the network, and as it is received, stored, and processed.

*MORE WORK TO BE DONE HERE - DIAGRAM?*

# Design for a long life

An IoT solution is an expensive investment, and as such it should be designed to last (possibly for decades.) The nature of technology is that hardware has a limited lifetime before it becomes obsolete. While switching a device simply because a newer model is available is not necessarily a desirable approach, innovations that increase the functionality or speed of devices can enable more advanced scenarios. An IoT system should be designed to support not only the current state of the art crop of devices, but also allow for the integration of sensors, hardware, and capabilities (including new software and infrastructure) that may become available in the future.

# Build to the reference architecture

*TBD - DIAGRAM*

# Further information
