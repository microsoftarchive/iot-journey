# Journal: Device Provisioning

We use the term *provisioning* to describe adding a new device to an IoT solution. This scenario also encompasses *deprovisioning* a device &mdash; that is, taking it out of service.

- [Common requirements](#common-requirements)
- [Fabrikam requirements](#fabrikam-requirements)
- [Exploring solutions](#exploring-solutions)
- [Reference implementation](#reference-implementation)


## Common requirements

- Only authorized users, such as service technicians, can provision a device.
- Authorization is typically *organizational*. That is, users log in with organizational credentials.
- Provisioning might be done by a field technician when the device is installed, or at manufacturing time.
- Devices must be authenticated in order to send or receive data.
- Devices can be revoked.
- Devices can be temporarily disabled (that is, taken offline for whatever reason) and then re-enabled.
- If a device stores a crypto key or access token, and the device is compromised, the leak should not compromise other devices. 
- The system will typically store metadata about a device, such as physical location, service records, etc. Part of provisioning is putting the device ID into a database. In our implementation, we call this the *device registry*.  

If the IoT solution uses a field gateway to communicate with the cloud backend, then the field gateway is the component that authenticates with the cloud. In that case, you will need to manage two sets of identities: The identity of the field gateway, and the individual device IDs.

## Fabrikam requirements

:memo: For more about our Fabrikam scenario, see [Introducing the Journey][00-intro]. 

For our scenario, we are assuming the following:

- A technician will physically install the device in a building.
- Each device will already have an identifier (a GUID that uniquely identifies the device).
- The device registry must store the location of the sensor (building and room number).
- There is no field gateway.


## Exploring solutions

The big challenge for provisioning is authentication. 

We are using Azure Event Hubs for event ingestion. (See the event ingestion [journal entry][event-ingestion-journal] for context.) Event hubs lets you define *shared access policies*. For example, you can define a Send policy that can send messages to the hub, but cannot listen or manage.  

Given the choice of Event Hubs, we looked at two approaches to authentication. 

1. The device stores the primary key for the shared access policy. 
	- This is a **bad idea**. An event hub can have at most 12 policies, so they key would have to be shared among many devices. If one device is compromised, all the others with the same key are compromised. The only way to revoke a device, is to regenerate the key.
2. The device stores a [SAS token][event-hub-publisher-policy]. This approach is better, because:
	- SAS tokens are tied to specific *publishers*. In this case, the device is the publisher and the unique device ID is used as the publisher ID.
	- Devices do not share SAS tokens.
	- If a device is compromised, you can revoke that device (by blacklisting the device ID) without affecting other devices.  

Drawbacks:
- SAS tokens are tied to a particular event hub, which may limit horizontal scaling.
- Event Hubs does not support token exchange, where the server rolls over tokens and the device exchanges its current token for a new token.   
- You might want more granular permissions than shared access policies allow.

## Reference implementation

For the reference implementation, we created a simple web API, which can be hosted as a Azure App Service web app. A field technician would log into the app, enter the device ID, and receive a SAS token for the device. (We're assuming the technician has some way to get the SAS token into the device's firmware.) 


[event-ingestion-journal]: 01-event-ingestion.md
[event-hub-publisher-policy]: http://blogs.msdn.com/b/servicebus/archive/2015/02/02/event-hub-publisher-policy-in-action.aspx