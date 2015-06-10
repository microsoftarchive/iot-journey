# Introducing the Journey

Smart buildings based on IoT solutions are becoming increasingly commonplace. A smart building tracks and controls the internal environment. In particular thermostatic and photo-electric devices can monitor factors such as the temperature, ambient lighting, and humidity to help ensure the comfort of the occupants. A smart building can also incorporate safety devices, such as smoke detectors and intruder alarms. The data captured from the various monitoring devices can be used to send commands to other devices such as air conditioning units, lighting dimmers, sprinklers, and automatic fire doors, as well as alert the appropriate authorities if an emergency situation is detected.

> To find out what we mean by "IoT solution", see [_What is an IoT solution?_][intro-to-iot].

## Meet Fabrikam

Fabrikam is a startup software development company with a dozen employees. The developers all have a background with .NET and Microsoft Azure. They also have a growing interest in technologies such as NodeJS, Hadoop, and Java.

The company vision is to become a "smart-building service provider"; a company that provides "environmental monitoring services" on a contractual basis. Initially, they intend to offer their services to owners of high-end apartment buildings. Once contracted, they will install devices in the apartments to be monitored. These devices will report on temperature, humidity, smoke alarm and other environmental conditions.

Fabrikam will use the data collected from the devices to provide various services to the building owners. They imagine services that include:
- Cost-saving on utilities
- Fire alerts
- Flood alerts (broken pipes, slow leaks, etc.)
- Unexpected temperature changes (indicating a broken HVAC)

In addition, Fabrikam wants to give individual tenants the ability to view
the current state of their own apartment through a mobile app.

Fabrikam landed their first contact before they even wrote a line of code.
Now they now have a few months to roll out a system to production.

## The Journal

The purpose of this journal is to record the steps that the developers at Fabrikam took, document the decisions that they made, and keep a log of any issues that they encountered. The result is intended to a story that tells of the trials and tribulations of the developers as they implemented the system.

Note that the purpose of this journal is not necessarily to record a _realistic_ scenario, but rather focus on the issues that are _representative_ of the actual challenges that are likely to be met by most organizations faced with building an IoT system for the first time. Additionally, the specification of the system being developed by Fabrikam is not yet fixed; features might be removed or replaced and new features added. We aim to use the [backlog][] to record and drive these changes.


## The People

A well-balanced team for building an IoT solution is composed of several roles. The roles that are most important for your solution may be different from the ones discussed here.

The people working at Fabrikam each bring their own unique perspective.

- **Beth** is the _business manager_ for Fabrikam.
	She understands Fabrikam's target market, the resources available to the company, and the goals that they need to meet. She has a strategic view and an interest in the day-to-day operations of the company. She is deeply concerned about customer experience.
	
	> ![Beth](media/PersonaBeth.png) 
	"We need to take risks to be successful; but they need to be the right risks. 
	I want to make sure that our team can respond quickly to customer needs without sacrificing future flexibility."

- **Markus** is the software developer responsible for the _devices_.
	He is analytical, detail-oriented, and methodical. He has a lot of experience with embedded systems. He is concerned about unnessary abstractions and inefficiencies in the code.
	
	> ![Markus](media/PersonaMarkus.png) 
	"The devices we deploy are likely to be in the field for years. I want to get this right the first time."

- **Jana** is the software developer responsible for the _cloud-hosted services_.
	She has a background with high-scale consumer-facing systems. She favors composable designs and that be evolved over time. She is constantly looking for ways to improve the development process.

	> ![Jana](media/PersonaJana.png) 
	"We need to make this system available to the customer as soon as possible so that we can get feedback."

- **Poe** is an IT professional who's an expert in deploying and running applications in the cloud.
	He believes that it's important to work closely with the development team. He's also concerned with ensuring that Fabrikam's system meets it's published service-level agreements (SLA).
	
	> ![Poe](media/PersonaPoe.png) 
	"Availability and reliability are critical to our customers. We can't afford to have downtime for upgrades."

- **Carlos** is a data scientist. 
	He's interested discovering new insights that Fabrikam can leverage for its customers. He wants to bring the latest thinking about data science and machine learning to the company.
	
	> ![Carlos](media/PersonaCarlos.png) 
	"I'm excited about this company. The sooner that we can get real data from the system, the sooner we can bring real insights."

## The Initial Release

The engineering team has established the following high-level goals for the initial production deployment for their first customer.

- Based on the number of apartment buildings and number of devices needed per building, the system needs to support **100,000 provisioned devices**.
- Each device will be sending approximately **1 event per minute**. This means the system will need to ingest **~ 1,667 events per second**.
- Authorized users need to be able to provision and de-provision individual devices.
- The customer requires that all telemetry (the events sent from the devices) needs to be stored indefinitely.
- The customer wants to be able submit Hive queries from time to time, so the stored telemetry needs to be "Hive friendly".
- Authorized users need to be able to see an aggregated recent state for a given building. For example, what is the average temperature in Building 25 currently? 
- The customer also has a number of legacy devices collecting data that they would like to feed into Fabrikam's system. However, these devices don't conform to any standard protocol for transmitting and receiving data.
- While not necessarily a customer requirement, Fabrikam wants to avoid any downtime after the initial deployment. This includes downtime for system upgrades. They are interested in continuous deployment. 

The team proposed the following logical architecture:

![plan for the logical architecture](media/00-introducing-the-journey/logical-architecture.png)

- _Devices_ represent both the devices provided by Fabrikam as well as those legacy devices the customer has.
- _Cloud Gateway_ is a cloud-hosted service responsible for authenticating all devices. It is also where the system will translate messages for those devices that don't speak the standard language.
- _Event Processing_ is the part of the system that ingests and processes the stream of events. It is a composition point in the architecture allowing new downstream components to be added later.
- _Warm Storage_ will only store the recent aggregated state for each building. It will receive this state from Event Processing. It is "warm" because the data should be recent and easily accessible.
- _Cold Storage_ is where all of the telemetry is stored indefinitely.
- _Device Registry_ knows which devices are provisioned. Its data is used by the Cloud Gateway as well as in the Dashboard.
- _Provisioning UI_ is a user interface for provisioning and de-provisioning devices.
- _Dashboard_ is a user interface for exploring the recent aggregate state.
- _Batch Analytics_ anticipates the Hive queries that the customer will want to run from time to time.

## The Logical Process for Handling Events

The developers constructed a logical model to describe the sequence of operations for handling events. This model consists of 4 phases:

- **Ingestion** Receiving events from devices. Events must be received reliably and in good time (not necessarily real-time).
- **Processing** Handling events once they have been received. Processing could include operations such as filtering and aggregating event data, or simply passing the received raw data through to another system for storage and analysis.
- **Storage** Recording the processed event data in safe, reliable storage. The storage system must be capable of handling potentially large volumes of incoming data as well as supporting queries that can analyze this data.
- **Interaction** Providing mechanisms to enable operators and data analysts to examine the event data and utilize this information to draw meaningful conclusions about the state of devices and buildings.

The intention is that each of these operations can be implemented by using a variety of different technologies, and the developers did not want to be bound unnecessarily to a specific tool or service until they had established its suitability and capabilities.

## The Steps

The developers decided to approach the system design and implementation through a series of incremental steps. This strategy enables them to evaluate the appropriate technologies as well as build something concrete that can form part of the solution. The steps that they defined are:

1. **[Capturing event data and saving it in its raw format to cold storage][01-cold-storage]**. This step enables developers to determine an approach to ingesting data, performing the simplest of processing, and saving it for subsequent analysis. Because the customer requires all event data to be stored indefinitely, the volume of data held in cold storage could become very large. Cold storage must therefore be inexpensive.

2. **[Saving event data to warm storage for ad-hoc exploration][02-warm-storage-ad-hoc]**. This step considers how to store data for warm processing. Analysts and operators performing ad-hoc queries are unlikely to require the details of every historical event, so warm storage will only record the data for *recent* events. This will enable queries to run more quickly, and be more cost effective for expensive data stores that support the features required to run complex queries.

3. **[Saving event data to warm storage for generating aggregated streams][03-warm-storage-aggregated]**. This step is concerned with saving event data for warm processing that can generate derivative events, such as raising an alert if the average temperature in an apartment consistently exceeds a pre-defined limit over a specified time (for example, an apartment reports temperatures in excess of 30 degrees Celsius for the last 5 minutes). This information could indicate a fault in the system, such as a broken air conditioning unit, and an alert could be raised to send an engineer to investigate. As with the previous step, these queries only require access to recent data, but the processing is more defined.

4. **[Provisioning new devices][04-provisioning-devices]**. TBD

5. **[Translating event data for legacy devices][05-translating-event-data]**. TBD

As we progress on this journey, we will fill out the details of each step by adding new [milestones][]. Each milestone will have a specific set of goals, deliverables, and target date. We'll also create an entry in this journal for each milestone.

[intro-to-iot]: ../articles/what-is-an-IoT-solution.md
[backlog]: https://github.com/mspnp/iot-journey/issues
[milestones]: https://github.com/mspnp/iot-journey/milestones
[01-cold-storage]: https://github.com/mspnp/iot-journey/docs/journal/01-cold-storage.md
[02-warm-storage-ad-hoc]: https://github.com/mspnp/iot-journey/docs/journal/02-warm-storage-ad-hoc.md
[03-warm-storage-aggregated]: https://github.com/mspnp/iot-journey/docs/journal/03-warm-storage-aggregated
[04-provisioning-devices]: https://github.com/mspnp/iot-journey/docs/journal/04-provisioning-devices
[05-translating-event-data]: https://github.com/mspnp/iot-journey/docs/journal/05-translating-event-data