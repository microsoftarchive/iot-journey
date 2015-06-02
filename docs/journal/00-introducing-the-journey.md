# Introducing the Journey

We want to tell a story about a fictitious company trying to build an IoT solution.

> To find out what we mean by "IoT solution", see [_What is an IoT solution?_][intro-to-iot].

In order to tell the story, we'll first establish a scenario that reflects common business requirements.
The scenario for our fictitious company is the prologue to the story. 
The scenario is not designed to be _realistic_, but rather _representative_ of actual challenges.
This scenario will drive our [backlog][]. 
As we progress on this journey, we will expand the scenario by adding new [milestones][]. 
Each milestone will have a specific set of goals, deliverables, and target date.
We'll also create an entry in this journal for each milestone.

## Meet Fabrikam

Fabrikam is a startup with a dozen employees. 
They have a background with .NET and Microsoft Azure. 
They also have a growing interest in technologies such as NodeJS, Hadoop, and Java.
They are planning to become a "smart-building service provider"; a company that provides "environmental monitoring services" on a contractual basis. 
Initially, they intend to offer their services to owners of high-end apartment buildings.
Once contracted, they will install devices in the apartments to be monitored.
These devices will report on temperature, humidity, smoke alarm and other environmental conditions.

Fabrikam will use the data collected from the devices to provide various services to the building owners.
They imagine services that include:
- Cost-saving on utilities
- Fire alerts
- Flood alerts (broken pipes, slow leaks, etc.)
- Unexpected temperature changes (indicating a broken HVAC)

In addition, Fabrikam wants to give individual tenants the ability to view
the current state of their own apartment through a mobile app.

Fabrikam landed their first contact before they even wrote a line of code.
Now they now have a few months to roll out a system to production.

## The People

A well-balanced team for building an IoT solution will be composed of several roles. 
The roles that are most important for your solution may be different from the ones discussed here.
The people working at Fabrikam each bring their own unique perspective.

- **Beth** is the _business manager_ for Fabrikam.
	She understands Fabrikam's target market, the resources available to the company, 
	and the goals that they need to meet. She has a strategic view and an interest in 
	the day-to-day operations of the company. She is deeply concerned about customer 
	experience.
	
	> ![Beth](media/PersonaBeth.png) 
	"We need to take risks to be successful; but they need to be the right risks. 
	I want to make sure that our team can respond quickly to customer needs without 
	sacrificing future flexibility."

- **Markus** is the software developer responsible for the _devices_.
	He is analytical, detail-oriented, and methodical. 
	He has a lot of experience with embedded systems.
	He is concerned about unnessary abstractions and inefficiencies in the code.
	
	> ![Markus](media/PersonaMarkus.png) 
	"The devices we deploy are likely to be in the field for years. I want to get 
	this right the first time."

- **Jana** is the software developer responsible for the _cloud-hosted services_.
	She has a background with high-scale consumer-facing systems. She favors composable
	designs and that be evolved over time. She is constantly looking for ways to improve
	the development process.

	> ![Jana](media/PersonaJana.png) 
	"We need to make this system available to the customer as soon as possible so that we can get feedback."

- **Poe** is an IT professional who's an expert in deploying and running applications in the cloud.
	He believes that it's important to work closely with the development team. 
	He's also concerned with ensuring that Fabrikam's system meets it's published service-level agreements (SLA).
	
	> ![Poe](media/PersonaPoe.png) 
	"Availability and reliability are critical to our customers. We can't afford to have downtime for upgrades."

- **Carlos** is a data scientist. 
	He's interested discovering new insights that Fabrikam can leverage for its customers. 
	He wants to bring the latest thinking about data science and machine learning to the company.
	
	> ![Carlos](media/PersonaCarlos.png) 
	"I'm excited about this company. 
	The sooner that we can get real data from the system, the sooner we can bring real insights."

## The Initial Release

The engineering team has reviewed the customer proposal and they have established 
the following high-level goals for the initial production deployment.

- Based on the number of apartment buidlings and number of devices needed per building, 
the system needs to support **100,000 provisioned devices**.
- Each device will be sending approximately **1 event per minute**. This means the system will need to ingest 
**~ 1,667 events per second**.
- Authorized users need to be able to provision and deprovision individual devices.
- The customer requires that all telemetry (the events sent from the devices) needs to be stored indefinitely.
- The customer wants to be able submit Hive queries from time to time, so the stored telemetry needs to be "Hive friendly".
- Authorized users need to be able to see an aggregated recent state for a given building. 
For example, what is the average temperature in Buidling 25 currently? 
- The customer also has a number of devices collecting data that they would like to feed into Fabrikams system. 
However, these devices don't speak a standard protocol.
- While not necessarily a customer requirement, Fabrikam wants to avoid any downtime after the initial deployment. 
This includes downtime for system upgrades. They are interested in continuous deployment. 

The team proposed this logical architecture.

![plan for the logical architecture](media/00-introducing-the-journey/logical-architecture.png)

- _Devices_ represent both the devices provided by Fabrikam as well as those legacy devices the customer has. 
(We're going to simulate the events from the devices in our implementation.)
- _Cloud Gateway_ is a cloud-hsoted service responsible for authenticating all devices. 
It is also where the system will translate for those devices that don't speak the standard language.
- _Event Processing_ is the part of the system that ingests and processes the stream of events. 
It is a composition point in the architecture allowing new downstream components to be added later.
- _Warm Storage_ will only store the recent aggregated state for each building. 
It will receive this state from Event Processing. It is "warm" because the data should be recent and easily accessible.
- _Cold Storage_ is where all of the telemetry is stored indefinitely.
- _Device Registry_ knows which devices are provisioned. Its data is used by the Cloud Gatewat as well as in the Dashboard.
- _Provioning UI_ is a user interface for provisioing and deprovisioning devices.
- _Dashboard_ is a user interface for exploring the recent aggregate state.
- _Batch Analytics_ anticipates the Hive queries that the customer will want to run from time to time.

[intro-to-iot]: ../articles/what-is-an-IoT-solution.md
[backlog]: https://github.com/mspnp/iot-journey/issues
[milestones]: https://github.com/mspnp/iot-journey/milestones
