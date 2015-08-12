# Start small, think big

It is very easy to become overwhelmed by the sheer volume of data and the number of devices that an IoT system might be expected to handle. In addition, it can be hard to predict in advance which sensors and devices might provide the data that your system requires. 

Rather than attempting to build a system that solves every problem at once, start end-to-end prototyping with a small number of devices, but focus on designing an architecture that will scale, is low-latency, and that can handle extreme hardware and software heterogeneity. Consider that some critical data might require immediate action while a large volume of information may need to be stored for subsequent analysis and decision-making. Furthermore, identifying how data flows from devices all the way through the system to the analytics processing and business domain logic can help to highlight issues that might otherwise be missed.

Implementing an initial subset of the system will help to highlight where possible issues are likely to occur. These issues include resolving device identity, management, update, and deployment of devices, and security concerns. It is easier to address these issues in the small scale. Be prepared to evaluate whether the prototype meets expectations, make any necessary adjustments, and then document the lessons learned so that you can apply them to the large scale.

# Focus on telemetry first

Devices and sensors are intended to report information about their state, while many devices can also accept commands to change their state controlled by the business logic of the system. It can be inefficient to try and incorporate complex state-changing business processes in an IoT system until the data reported by devices and device capabilities are fully understood. Instead, start by focussing on the telemetry provided by devices (not only the state information that a device reports, but also diagnostics that help to establish device health). Privacy and security issues tend to be simpler for telemetry than for command and control, so this approach enables you to focus on security and manageability of the system before becoming embroiled in detailed transformational logic.

Prototyping with telemetry can also give you an idea of how your system will respond under load. The system must be capable of ingesting data quickly, performing hot path processing of this data within the expected time-frame, and store potentially large volumes of data for cold path analysis without becoming overwhelmed.

# Don't interrupt the hot path

Critical state information retrieved from devices may need to be handled quickly. Don't create processing bottlenecks in the hot path. For example, don't transform and manipulate the raw data unnecessarily unless the system can handle such transformations at scale. Additionally, store data for future analysis in its raw form and use the cold path processing to perform any required data conversions.

# Handle defense in depth

Consider security, identity, and management from the very start; this is not something that can be added in later. Think about security on the device itself, as data is transferred over the network, and as it is received, stored, and processed. In particular, consider the following aspects:

- Physical security and tamper detection of all devices in the field.

- Firmware security and secure boot to prevent hardware being compromised.

- Network protocol security to protect data in flight.

- Application security to prevent loss and leakage of sensitive information.

- Identity management for devices (prevent unexpected, rogue devices from being introduced into the system) and users.

- Data privacy protection and controls to ensure that data is stored securely.

# Design for a long life

An IoT solution is a significant investment. Once a device has been deployed to the field it might be there for a long time; don't necessarily expect that you will be able to replace it with newer hardware or update the firmware easily. Consequently, an IoT system should be designed to last, possibly for decades. 

You should also consider that the nature of technology is such that newer, more powerful devices can become available at relatively short notice. Innovations that increase the functionality or speed of devices can enable more advanced scenarios as the system evolves and expands. An IoT system should therefore be designed to support not only the current state of the art devices, but also allow for the integration of sensors, hardware, and capabilities (including new software and infrastructure) that may become available in the future.

# Design for composability

Design the system with clear, well-defined interfaces to enable composability. This will allow the system to be easily extended by integrating new devices, architectural components, processing capabilities, and services. Analysis models and decision-making logic can be more easily modified and evolve to adapt to changing business requirements.