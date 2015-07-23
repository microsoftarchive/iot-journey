# Introducing the Journey

This project has two aspects:

- Guidance on designing an IoT solution, with a particular focus on the cloud back end.
- A reference implementation.

The reference implementation is designed to test and validate our assumptions, and to keep our guidance honest. 

> To find out what we mean by "IoT solution", see [_What is an IoT solution?_][intro-to-iot].

## The problem space  

We've identified some common scenarios and challenges in IoT, which fall naturally into several broad categories.  

**Managing devices**. This includes provisioning new devices, taking devices offline (for maintenance or retirement), and device configuration.

Devices may need to be provisioned in the field (e.g., installing sensors in a building), using a self-serve model. That is, the technician who installs or services the device can provision it right there in the field, possibly using a mobile app. In that scenario, it may be important to protect the device from being reconfigured by hackers. 

**Ingesting data**. The most basic task of an IoT system is getting data into the back end, reliably and at scale. Considerations include:

- The expected data rate (how many devices; how many events per device). 
- Physical network requirements.
- Message protocols. (HTTP? [AMQP](https://www.amqp.org/)? Others?) 
- Protocol translation. In some cases, you might need to translate output from the devices into the expected ingestion format. For example, you might have legacy devices that use non-standard protocols.
- Authentication. Only legitimate devices should be able to send data.  
  
During development, it is common to use a **device simulator**, which feeds simulated data to the system. A simulator is also useful for load-testing the back end.

**Consuming data**. Once the data is in the system, what are you doing with it? Here are some typical scenarios that we've identified:

- Short-term data aggregation: "In the last 5 minutes, what was the average temperature?"
- Critical alerts: "This unit is overheating!"
- Query the data (not in real time): "In the past week, how many times did the temperature fall outside of a given range for more than ten minutes?"
- Long-term storage
	- Auditing 
	- History
- Visualization. Business reporting and charting, dashboards, etc.

**Command and control**. In some IoT scenarios, a device can receive outbound messages from the back end. This doesn't apply to every IoT solution. Currently, our example scenario does not include command and control.   

**Backend services**. These are some things that you might build on the back end:

- Dashboards.
- System administration, to control who has access to which parts of the system. 
- APIs. You might create API layers so that consumers don't have to deal with raw data. APIs can also help protect privacy, and handle authorization.


**Cross-cutting concerns**. 

- **Security.** Security must be baked into the entire system. IoT security is a huge topic, and the industry is evolving quickly. (See [Introduction to IoT Security](../articles/introduction-to-IoT-security.md).)
- **Multi-tenancy.** It is quite common for IoT solutions to have multiple users/customers. When a customer logs into their dashboard, they should see their devices only. 
- **Cloud readiness.** The solution must be reliable and scalable.  
    
## The example scenario

To motivate the reference implementation, and to make things more concrete, we chose an example scenario, based on the idea of *smart buildings*.

A smart building tracks and controls its internal environment. Sensors monitor the temperature, ambient light, and humidity, to help ensure the comfort of the occupants. A smart building can also incorporate safety devices, such as smoke detectors and intruder alarms. Data captured from these monitoring devices can be used to control air conditioning, lighting, sprinklers, automatic fire doors, or to alert the authorities if an emergency situation is detected.

Here are the scenario parameters that we decided on:

- Fabrikam is a software development company that provides environmental monitoring services on a contractual basis. They will manage the cloud service on behalf of customers.
- The system should support **100,000 provisioned devices**. We'll use a simulator to simulate device events.
- Each device will send approximately **1 event per minute**. This means the system will need to ingest **~ 1,667 events per second**.
- Authorized users can provision and de-provision individual devices. 
- All telemetry (the events sent from the devices) is stored indefinitely.
- We want the ability to submit Hive queries on the stored telemetry data.
- Authorized users can see an aggregated recent state for a given building. For example, what is the average temperature in Building 25 currently? 
- The service will be multi-tenant. Users might include the owner of a particular building, a resident, a building management company, etc.      
- The system can support legacy devices that don't conform to any standard protocol for transmitting and receiving data. (Protocol translation.)
- The solution should support continuous deployment, to avoid downtime during upgrades.

Our goal was to make the scenario *representative* of typical IoT solutions, so the lessons we learn will apply more broadly.  


## The Journal

We are using this journal to record the decisions that we make for each scenario. In our view, there isn't a single "right" solution (although there may be wrong solutions!). Therefore, our reference implementation is most useful in the context of the decisions that we made and the alternatives that we explored.      

You can follow our progress by reading the journal entries, or dive into our [backlog][]. We welcome feedback. 


## Logical architecture

Before we even get to specific implementations, here is the logical architecture that we are proposing:

![plan for the logical architecture](media/00-introducing-the-journey/logical-architecture.png)

- _Cloud Gateway_ is a cloud-hosted service responsible for authenticating devices and (possibly) translating messages for devices that don't speak the standard language.
- _Event Processing_ is the part of the system that ingests and processes the stream of events. It is a composition point in the architecture allowing new downstream components to be added later.
- _Warm Storage_ holds the recent aggregated state for each building. It will receive this state from Event Processing. It is "warm" because the data should be recent and easily accessible.
- _Cold Storage_ is where all of the telemetry is stored indefinitely.
- _Device Registry_ knows which devices are provisioned. Its data is used by the Cloud Gateway as well as in the Dashboard.
- _Provisioning UI_ is a user interface for provisioning and de-provisioning devices.
- _Dashboard_ is a user interface for exploring the recent aggregate state.
- _Batch Analytics_ anticipates the Hive queries that the customer will want to run from time to time.

## Handling event data

We also created a logical model to describe the data flow through the system. 

1. **Ingestion**. Receiving event data from devices. Events must be received reliably and in good time (not necessarily real-time).
2. **Processing**. This could mean filtering and aggregating event data, or simply passing the raw data through to another system for storage and analysis.
3. **Storage**. Putting the processed event data in safe, reliable storage. The storage system must handle potentially large volumes of incoming data, yet be flexible enough to support complex queries efficiently.
4. **Interaction**. Enabling operators and data analysts to examine the event data and draw meaningful conclusions about the state of devices and buildings.

The intention is that each of these steps can be implemented using a variety of different technologies. We don't want to commit to a specific tool or service until we have established its suitability. (With the general caveat that we are building this for Microsoft Azure.)

## Implementation phases

We are approaching the project in phases, building a functional part of the system in each phase. This strategy lets us evaluate the appropriate technologies and quickly deploy something concrete. These phases are orthogonal to the data flow model above. Each phase might touch several stages of the data flow.

1. **[Capturing event data][event-ingestion]**. It sounds obvious, but the most basic task for an IoT solution is getting event data into the cloud.

1. **[Saving raw event data in long-term storage][long-term-storage]**. Assuming that all event data must be stored indefinitely, the volume of data held in cold storage could become very large. Cold storage must therefore be inexpensive.

1. **[Saving event data to warm storage for ad-hoc exploration][ad-hoc-exploration]**. This phase is concerned storing data for warm processing. Analysts and operators performing ad-hoc queries are unlikely to require the details of every historical event, so warm storage will only record the data for *recent* events. This will enable queries to run more quickly, and be more cost effective for expensive data stores that support the features required to run complex queries.

1. **Saving event data to warm storage for generating aggregated streams**. This phase considers the issues around generating information derived from the original event data. Initially, this derivative information is a rolling record of the average temperature reported by all devices in each building over the previous 5 minutes, but additional aggregations may be added as required by the client. As with the previous phase, these queries only require access to recent data, but the processing is more defined.

1. **Provisioning new devices**. TBD

1. **Translating event data for legacy devices**. TBD

Other phases may be added as development progresses.

As we proceed on this journey, we will fill out the details of each phase by adding new [milestones][]. Each milestone will have a specific set of goals, deliverables, and target date. We'll also create an entry in this journal for each milestone, describing the decisions we made, the reasons behind them, and any specific issues that we uncovered.

[intro-to-iot]: ../articles/what-is-an-IoT-solution.md
[backlog]: https://github.com/mspnp/iot-journey/issues
[milestones]: https://github.com/mspnp/iot-journey/milestones
[event-ingestion]: 01-event-ingestion.md
[long-term-storage]: 02-long-term-storage.md 
[ad-hoc-exploration]: 03-ad-hoc-exploration.md
