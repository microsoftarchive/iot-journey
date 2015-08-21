# IoT security: What are the challenges?

IoT encompasses the very small (embedded devices) and the very large (clusters of VMs); the very local (sensors in buildings, machines, or cars) and the global (the cloud). So it should be no surprise that IoT also faces a wide range of security challenges.

In this article, we present a broad overview of some of the these challenges. 

None of the challenges described here are really “new.” Devices talking to networks is not a new phenomenon. What may be new is the scale &mdash; the potential number of devices involved, all connected to the cloud. Depending on your background, some of these points may be obvious, others may be less familiar.

## Risks

IoT devices are often attached to things that can be dangerous in the physical world, like vehicles or machines, perhaps running 24/7 without human supervision. A security flaw can mean physical damage and loss of life.

Even if a device is purely a data transmitter, with no command-and-control, data from the device may trigger an action by some other actor. (Somebody looks at a reading on a dashboard and responds.) Those actions can have real-world consequences on machines, infrastructure, or people.  

## Device constraints

IoT devices are often special-purpose hardware, rather than general computing devices. They may have very tight constraints on power, battery life, memory, or compute power. (Can your device actually run a crypto algorithm, alongside the other CPU work it’s doing?)

Your system must cater to the constraints of your devices. If you have a low-powered device that does not support AES-256 encryption, you may have to build a different security method.  

To compound matters, the devices might already be deployed in the field. Legacy devices are generally designed for closed networks, not the Internet. And many digital components and control systems never get updated or patched. In short, you need to design your IoT solution for the devices you have, not the devices you wish you had.

In some scenarios, a field gateway can compensate for these issues. 

## Device vulnerability

Devices may be placed in the open, and not under the physical control of a user. That makes them targets for malicious tampering, or simple wear-and-tear from the elements. Analog components in particular may fail or give defective readings. 

Also, many IoT devices take readings from the physical world in a way that doesn’t require special access or authentication. To “hack” a temperature sensor, I can just hold a lighter next to it. 

Mitigations might include physically securing the device; making the device smarter (add a proximity detector to the temperature sensor?), or analyzing the data on the backend for anomalous or implausible input.

## Networking

A device might have intermittent network connectivity, either due to power constraints or because the device roams. This can make it hard to deliver messages reliably.
 
- How do you establish a connection and address the device?
- How do you prevent DOS attacks on the service, on the field gateway, and on the device itself (if the device listens for connections)?
- Do I need transport level encryption? If so, does my device support it, and what is the impact on the processor as it does the encryption?

## Device identity

In traditional computing devices, such as PCs and phones, identity is tied to the device. In IoT, a device might be part of a larger system who identity persists beyond the lifetime of the device.

Consider an industrial machine that has several sensors. Even if you replace all of the sensors, it’s still the same machine. Many of the decisions around authentication and authorization, for example, may be tied to the identity of the larger system, not the individual components. 

## Authentication 

The web and the PKI infrastructure are built on the idea that it should be relatively easy to establish trust. You point your browser at Facebook or your banking app, and the browser trusts the site because it has a certificate signed by a trusted CA. There is also a “fail-stop” in the form of the user, who can close the browser, clear the credential cache, etc., if a site looks suspicious.

In IoT, devices are mostly autonomous. Once you provision a device, establishing an authenticated channel must be automatic. On the other hand, in *most* scenarios, an IoT device doesn’t need to (and shouldn’t) establish trust with a lot of heterogeneous services. Generally, a device is paired with a service. In this context, certificate exchange is a relatively expensive operation and may not be the best approach. For more thoughts on this topic, see [Service Assisted Communication for Connected Devices](http://blogs.msdn.com/b/clemensv/archive/2014/02/10/service-assisted-communication-for-connected-devices.aspx) (blog post).

Some other considerations:

- Revoking a compromised device.
- Rolling over access tokens. 

## Authorization

When you consider authorization, remember that in addition to multiple roles, there may also be multiple *organizations*, including OEMs, suppliers, customers, vendors, and so forth. Consider a sensor in a vehicle. Someone makes the sensor. Someone else makes the vehicle. Other companies might service the vehicle, insure it, lease it, etc. Each of these might have legitimate reasons to access the device.

- Who is authorized to provision or service a device? 
- Who is authorized to view data from the device? 
- Should you create different views of the data, with different access rights? (If your car sends GPS data, who is allowed to see where you drove, versus just your mileage or average driving speed?)
- Who is authorized to send command and control messages?

## Privacy

With a phone or tablet app, there is a user who opts in to using the device. Obviously there are still serious privacy concerns, but at least the user can shut off the device. With IoT, data collection may be more intrusive, with sensors in the home, workplace, or public spaces, defaulting to “always on.” Data protection laws may limit how you can collect, store, and use the data. 

Multiple parties might consume the data, which ties in to the authorization issue. 

In some cases, the very presence or absence of data might need to be protected. For example, if a motion detector in your house is not registering any movement data, an attacker can conclude that nobody is home. Even if you encrypt all of your data, think about side-channel attacks.

## Trustworthiness

In a traditional client-server app, you can establish trust with a combination of trusted software, user credentials, and the user having physical control over the device. 

IoT devices interact with the physical environment, often without immediate human supervision. There may be layers of analog and mechanical components that sit between the sensors and the digital components. All of these are subject to interference. Cryptographic techniques won’t help if the data is compromised before it reaches the digital layer. Trustworthiness must include data cross-referencing and filtering, to help detect tampering.


