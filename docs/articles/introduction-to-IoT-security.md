# Introduction to IoT security

IoT systems can contain many moving parts, each of which need to communicate and coordinate with each other in a secure manner. In a highly connected environment, security is not simply a matter of protecting individual resources, but is also concerned with protecting the information that is transferred between those resources.

In an IoT solution, security and safety are commonly intertwined. Many IoT systems control critical operations and the core of industrial and civil infrastructure. A lack of security that could lead to unauthorized actions in a business system that might cause a degree of inconvenience. The same issue could could have lethal consequences if it occurs in an IoT solution. Consequently, digital security is increasingly interwoven with the physical safety of life and equipment.

Privacy is also a major concern. Many IoT systems provide very deep and near-real time insight into industrial and business processes, as well as into homes and the immediate personal environment. Leakage of personal data can be embarrassing (and costly), but might also be life-threatening; if the whereabouts of a vulnerable or important person is easily tracked then that person becomes a sitting target. 

The purpose of this document is not to describe how to protect information, devices, and other components in an IoT system, but rather to illustrate points that you must consider when implementing IoT security.

## Understanding possible attack surfaces

A good starting point for considering IoT security is to ensure that you are aware of the potential points of weakness in the system. The following diagram illustrates a generic IoT architecture, encompassing many devices interacting with a set of monitoring and control services running in the cloud.

This diagram is an abstraction of the system described in the document [What is an IoT Solution?][what-is-an-iot-solution]. The data from devices is typically passed to the services that comprise the IoT system through a field gateway and/or a cloud gateway. Commands from the IoT system services flow in the other direction. As an example, a smart car could be considered to be a field gateway with its own local control software gathering data from individual devices located on the vehicle, monitoring them, and sending commands back to them. The connection between a field gateway and/or local devices with a cloud gateway could be static or dynamic. For example, a field gateway that roams (such as a smart car) might switch to different cloud gateways as it changes location.

![Generic IoT Architecture][generic-iot-model]

Each of the elements in this model expose various vulnerabilities that you need to address.

### Sources of device vulnerabilities

Devices will be deployed to observe or control processes in environments that might not provide any significant protection against physical tampering or theft. An attacker could potentially replace the device with a spoof that captures sensitive information and leaks it to a third party or that operates maliciously. The replacement device could be programmed to stage a simple DoS attack by transmitting a large number of signals to the field gateway, effectively flooding that part of the system and rendering the field gateway from responding to requests from other devices or the control services.

Even if a device is physically secured, spoofing and impersonation could still occur, especially if communications with a field gateway are wireless. Many devices are cheap commodity items that do not include the capability to encrypt data so it could be easy to introduce a rogue device that assumes the identity of a valid one into the local network and starts to present bad data. Additionally, a DoS attack could be staged by using a transmitter that interferes with the frequency used by the local wireless network without requiring any other form of entry into the system. Furthermore, it could be possible for an observer to silently intercept traffic, resulting in disclosure of sensitive or valuable information. Some form of shielding may be necessary.

Devices that listen for incoming connections can be especially vulnerable unless these connections are authenticated in some way. Even then, a device can be made the target of a DoS attack simply by bombarding it with a large number of connection requests; even if these requests are recognized as invalid by the device, it can still be overwhelmed by the processing involved. 

To some extent, you can address these issues by following the [Service Assisted Communication][service-assisted-communication] pattern

Attacks do not have to be hi-tech. If a device exposes a physical control interface (such as a set of switches) it is necessary to ensure that this interface remains inaccessible to unauthorized access (don't let a stranger fiddle with the buttons). Even if a device does not have such an interface but measures some aspect of the physical environment, the device must be protected. For example, a fire detector that operates on heat sensitivity but that is artificially cooled might fail to trigger a fire alarm.

### Sources of field gateway vulnerabilities

A field gateway is usually deployed in a similar proximity to the devices with which it communicates and controls, and as a result could be subject to the same vulnerabilities as these devices. Physical protection is important, especially as a rogue gateway could affect a significant number of devices. For example, if the ECU for a car is compromised the car can be disabled.

The field gateway can act as a proxy for devices, passing device data to the control system and forwarding commands from the control system (via a cloud gateway if necessary). A field gateway therefore exposes two kinds of interfaces: internal device facing and external service facing. Many commodity devices only implement minimal forms of security. They might include some identity information in any messages that they send that the field controller can verify, but for or reasons of cost and speed they might not provide much else. For example, the electronic ignition timer on a car is unlikely to encrypt data that it sends to the ECU or decrypt any commands it receives. Therefore the field gateway may have to simply trust the data that it receives from devices. 

Even the very basic form of authentication with the field controller based on device identity can be problematic though. If a device is replaced in the field, the replacement part could have a different identifier, and the field gateway will need to be programmed to accept this new identity otherwise the device could simply be ignored.

The external service facing interface of a field gateway is expected to be more robust. This is effectively the interface to the world outside of the (hopefully) closed environment managed by the field gateway. Communications with the cloud gateway or control services must be protected (usually by using encryption) and authenticated (possibly by using some form of shared secret). Field gateways must also be able to identify themselves to the control system. This means that the same issue with replacement devices can occur at this level; if the field gateway for a site is legitimately replaced, then it may be necessary to inform the control system or cloud gateway of any change in identifier.

Another issue concerns the maintenance of field gateways. Depending on the environment, it might be necessary to connect local diagnostics and calibration systems to the field gateway (think of a garage mechanic looking for the causes of a malfunctioning engine on a car with an ECU). These interfaces might be physical, requiring an operator to plug in a cable, or they could be electronic supporting remote access. However, these interfaces must be protected to prevent misuse, such as the introduction of malicious firmware (for example, the [Stuxnet worm][stuxnet]).

### Sources of cloud gateway vulnerabilities

The cloud gateway is the remote receiving point for device data, and the remote sending point for device commands. The communications usually occur across a public network, although these communications should be protected as described above. There are two common scenarios that can be an important factor in determining the vulnerabilities of the communications with the cloud gateway.

- **Roaming**. If a field gateway or device has a static relationship with the cloud gateway then the pair may be able to establish some form of trust relationship based on identity (the field gateway or device knows which cloud gateway it expects to talk to, and the cloud gateway could have a list of registered field gateways and devices). If devices and field gateways can roam then this relationship no longer holds, and some alternative authentication strategy is required. This strategy might need to operate quickly and transparently (the occupant of a fast moving car using a cellphone does not expect to have an interrupted service or redial every time the car moves between the range of different phone masts).

- **Multiplexing**. A field gateway or device might need to connect to multiple cloud gateways simultaneously. For example, a commercial transport truck is a complex machine with elements that are themselves made up of complex machines; it has an engine, a trailer, a braking system, and it may have a cooling unit for perishable goods. The manufacturers of all these components are as interested as the truck manufacturer in getting live information about the performance of their products. Furthermore, the truck might be leased from a leasing company, it is insured with an insurer, it’s serviced by a service partner, and is operated by the transport company. Thus, all these parties and the manufacturer may eventually be interested in getting information about the status of various systems inside the truck without each requiring a separate physical communication path from the truck to the cloud system; the truck is unlikely to be fitted with multiple radio links to handle each item. The field gateway on the truck might need to ensure that the separate items data are only disclosed to the appropriate parties

### Sources of services vulnerabilities

A service is a software component or module that implements some sort of business capability and is interfacing with devices through a field- or cloud-gateway for data collection and analysis and/or for command and control. For the most part, industry best practice for how to secure distributed systems applies unchanged to these services. The main differences in an IoT system are concerned with the interfaces to the devices.

The purpose of an IoT service is to turn the flow of device data into analytics insight and consequential action, and possibly to provide an operator with the ability to visualize the data and send commands to control devices remotely. The complexity of interactions between cloud gateways, field gateways and services poses challenges in how to provide device identity to the service, how to make sure that the data that flows through the gateways maintains secure state through the pipeline as it is being consumed by the analytical services in the back end. The flow of commands from services to devices must also be protected; a series of commands that appear to originate from a valid service could be severely destructive if instead they emanate from a rogue system. Even commands sent from a valid service could be catastrophic if they were issued by a malicious operator. A service must have safeguards to ensure that operators are authenticated and authorized appropriately (in some cases, this may require authentication and authorizing multiple independent operators simultaneously; one to verify the actions of another).

## Understanding end-to-end security and safety concerns

Having summarized the points of vulnerability in a typical IoT solution, the purpose of this section is to consider the key aspects of security that span the end-to-end flow of data through the system.

### Trustworthiness

The nature of an IoT system means that all devices (including field gateways) operating in the field are potentially vulnerable. Depending on the level of physical security, they might be easily spoofed, replaced, or otherwise compromised. Therefore, the data emanating from devices should never be taken on trust. As an example, the Stuxnet worm worked by infiltrating specific controller devices, observing the typical behavior and output of these devices before reprogramming them to act in a destructive manner while replaying output that indicated that the device was behaving normally.

The data transmitted by a device to the IoT system must not be easily corrupted or tampered with. This will typically necessitate encrypting data that is visible on the public network. Devices might not support encryption natively, and might instead rely on a field gateway to perform this operation on their behalf. In this case, the device-to-field gateway connection must be shielded from outside interference.

*OTHER EXAMPLES?*

### Identity

Each device must provide verifiable identity information. The data from any device that is unrecognized by the system should be ignored (and trigger an operator alert). The solution must ensure that device identity is not disclosed to unauthorized parties, and cannot be easily duplicated. For this reason, device identity is likely to form part of the encrypted payload sent to the cloud gateway and services by the devices and/or field gateway.

This issue is complicated if devices can be replaced in the field. The new identity of the replaced device must be recognized as valid by the system, while ensuring that the replacement identity is not that of some spoof device. An alternative approach is to program the replacement device with the same identity of the original device; this approach highlights the need to keep device identities secure. Similar issues apply if the system supports provisioning of new devices, and deprovisioning of redundant devices.

*OTHER EXAMPLES?*

### Authentication and Authorization

Addressing trustworthiness and identity concerns can help to ensure that only valid devices can send data to an IoT system, but the IoT system itself needs to be protected. This can be handled by implementing authentication and authorization (most probably based on identity and shared secrets) as data enters the cloud gateway and/or services from the outside world. Techniques for achieving this are well documented and are not unique to IoT systems.

Authentication and authorization issues also apply to commands travelling from the services and cloud gateway back through the field gateway to devices; the solution must ensure that devices cannot be instructed to act in a destructive manner by an external service that mimics a bona fide part of the IoT system.

*OTHER EXAMPLES?*

### Peering

Many devices support peering, which enables other devices to establish direct relationships with them. There may be justifiable reasons for supporting peering in an IoT system, but uncontrolled peering must be prevented. For example, enabling a device to support a blue-tooth connection can render the device vulnerable to sniffers that operate in promiscuous mode, and may also provide a backdoor into the system for attackers that can connect to the device over this connection.

*OTHER EXAMPLES?*

### Privacy

Devices might capture a variety of information that should not always be disclosed to every possible valid recipient. This is especially true if the same device gathers data that is shared by a variety of systems (consider the multiplexing scenario in the [Sources of cloud gateway vulnerabilities][] section.

In some cases, the very presence or absence of data might need to be protected. For example, if a potential intruder is able to ascertain that movement detection devices inside a house are not registering and movement data, then there is probably no-one at home. In these cases, it could be necessary to camouflage this lack of events, possibly by generating traffic that appears to show activity to a casual observer, but that only an authorized recipient will recognize as being a series of null events.

*OTHER EXAMPLES?*

### Auditing

Due diligence may require recording the details of every event reported by all devices. These details must be sufficient to support a forensic investigation if necessary (for example, the data recorded by the instruments on the flight-deck of an airliner after an incident). However, the level of detail must be balanced with the need to preserve anonymity where appropriate (for example, the audit data captured by medical instrumentation devices should not include any information that could identify a patient).

*OTHER EXAMPLES?*

### ***OTHER CONCERNS?***

# Further information

- [Best practices for creating IoT solutions with Azure](http://blogs.microsoft.com/iot/2015/04/30/best-practices-for-creating-iot-solutions-with-azure/)
- [IoT Security Fundamentals](http://channel9.msdn.com/Events/Ignite/2015/BRK4553)
- [Service Assisted Communication principle](http://blogs.msdn.com/b/clemensv/archive/2014/02/10/service-assisted-communication-for-connected-devices.aspx)
- [Glossary of Terms][]
 

[generic-iot-model]: media/introduction-to-IoT-security/generic-architecture.png
[what-is-an-iot-solution]: ./what-is-an-IoT-solution.md
[service-assisted-communication]: ./TBD
[stuxnet]: https://en.wikipedia.org/wiki/Stuxnet
[Glossary of Terms]: ../reference/glossary.md