# Choosing an application layer protocol

A key requirement in designing an IoT solution is the ability for devices to communicate with each other and the system quickly and at scale.

In an IoT solution, communications tend to follow one or more of the following patterns:

- **Telemetry**. This is the one-way flow of information from a device passing information about the status of the device to a connected system.
![One-way flow of telemetry information from a device][Telemetry]

- **Request/Response**. A device might initiate a request that gathers information from elsewhere, possibly to use as a trigger to initiate a new activity.
![Two-way request/response message flow from a device][Request]

- **Notification**. The system can send information to the device to indicate a change of status that may require some action to be performed.
![Notification message from the system to a device][Notification]

- **Command** The system can send an explicit request to the device to perform an operation, and obtain a response confirming the new state.
![Command message from the system to a device, with response][Command]

# Factors to consider

When determining which application layer protocol to adopt for an IoT system you should consider the following factors:

- **Bandwidth requirements**. A protocol that utilizes a compact binary format for data will require less bandwidth than a more verbose protocol. Messages will be smaller and faster to transmit.

- **Connection reliability**. Devices might be connected via wired or wireless networks (local and wide-area) which could have varying reliability. If it is important that data is not lost, then the application layer protocol should provide a mechanism for identifying whether messages have been received and retransmitting any missing information.

- **Ability to handle bursts**. A large scale IoT system might incorporate many thousands or even millions of devices. The system might be subject to burst events causing a large volume of traffic that need to be handled extremely quickly (for example, devices monitoring parts of the power grid during a storm or other atmospheric event might indicate a sudden surge in electrical flow requiring that the system quickly reroutes and distributes the electrical supply to avoid catastrophic failure).

- **Message distribution**. A single message may need to be transmitted to multiple destinations. In this case, the application protocol should provide transparent and configurable multicast communications.

- **Bidirectional messaging**. A bidirectional protocol enables a sender and receiver to establish a private conversational channel for exchanging messages. This type of communication is advantageous for systems that require a fast request/response mechanism. Without bidirectional support, request/response communications can require establishing separate channels and a means of correlating messages.

- **Adherence to standards**. The application protocol should be capable of supporting not just existing devices but any new devices that might be introduced into the system in the future. These devices might be heterogeneous. Adopting a well-defined standards-based protocol that supports backwards compatibility can help to mitigate the chances of near to mid-term obsolescence.

- **Resource requirements**. A highly robust, fully featured application layer protocol can also be complex, requiring a large memory footprint and considerable processing capabilities of the devices using it. This can, in turn, affect the power consumption of devices. The restrictions of device capabilities and resources may necessitate using a simpler protocol.

- **Security**. In a closed network, security might not be a concern, but in a system that spans wireless or wide-area networks then security becomes important. In this environment, it is important that messages are not compromised, intercepted, or otherwise subject to tampering. Additionally, it may be necessary to authenticate message sources to ensure that rogue devices have not been introduced into the system.

# Common application layer protocols for building Azure IoT systems

There are many application layer protocols available for enabling communications in an IoT system. Most tend to be targeted at a specific set of scenarios. The following sections describe some of the protocols commonly considered for building IoT systems using Azure.

## AMQP

AMQP (Advanced Message Queueing Protocol) is an open standard (ISO/IEC 19464) intended to replace a plethora of existing, proprietary, messaging middleware solutions. It provides a wide range of features based on message queueing, including:

- Reliable message delivery (at least once, and at most once)
- Publish and subscribe messaging
- Fan out routing to multiple destinations based on topics or on header information included in messages
- Transport-level and user-level security
- Optional Support for transactional messaging (either as X/Open XA transactions or MS DTC transactions)

A message queue acts as a store of messages that are routed to one or more destinations. As such, AMQP supports asynchronous operations and does not require immediate continual connectivity between a sender and a receiver. When a sender posts a message, it is received by a service (an exchange) that routes the message to the appropriate queues. Receivers can connect to these queues and read the message:

![Structure of an AMQP system][AMQP]

Azure Event Hub is an implementation of AMQP messaging that provides the exchange service and the message queues to which other services can connect.

The format of an AMQP message is binary, for compactness. The data is held in the body of the message, and a header can contain metadata used for routing or to provide information about the structure of the data in the body.

Security is supported at the transport level through connection security based on [TLS][TLS] to protect data in-transit across the network, and user-level authentication based on [SASL][SASL] to ensure that all messaging participants are valid devices and services.

## HTTP

*TBD*
Synchronous connectivity. Requires building a custom service (web role or Azure web site) to act as a hub for receiving and processing data. Highly flexible, but might require significant development effort to get it right.

## MQTT

*TBD*
Message Queuing Telemetry Transport.

Lightweight, async, with minimal security. Mainly concerned with collecting device data (telemetry)

## CoAP

*TBD*
Constrained Application Protocol. Intended for low power (constrained) devices.

# Comparison of protocols

*TBD - NEED TO SORT OUT CRITERIA FOR THIS TABLE*

| Protocol | Messaging Pattern(s) | Messaging Reliability | Footprint | Security |
| --- | --- | --- | --- | --- |
| AMQP |
| HTTP |
| MQTT |
| CoAP |

# Combining protocols

A single protocol might not be suitable for every element in an IoT system. Locall connected devices that need to communicate directly with each other would benefit from using MQTT, but for transmitting data over a wider distance or connecting with other services, then AMQP or HTTP might be a better option.

*TBD - BRIEFLY SHOW GATEWAY ARCHITECTURE - Devices connected to MQTT hub acting as a gateway and protocol translator for an AMQP system*

# More Information

*TBD - LIST OF REFERENCES TO GO HERE*


[Telemetry]: Figures/Protocols/Telemetry.jpg
[Request]: Figures/Protocols/Request.jpg
[Notification]: Figures/Protocols/Notification.jpg
[Command]: Figures/Protocols/Command.jpg
[AMQP]: Figures/Protocols/AMQP.jpg

[TLS]: http://en.wikipedia.org/wiki/Transport_Layer_Security
[SASL]: http://en.wikipedia.org/wiki/Simple_Authentication_and_Security_Layer