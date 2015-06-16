# Overview of the Architecture and Components in the Sample Solution

The sample solution code (available [here](https://github.com/mspnp/iot-journey/tree/master/src)) simulates a collection of smart buildings that monitor temperature in each apartment. Each monitoring device submits a temperature event every second containing the following data:

- The device ID (each device has its own unique ID).

- The currently observed temperature in degrees Celsius.

- The date and time of the observation.

The data for all devices is directed towards Azure Event Hub. From here, the data can take several paths:

1. The data can be used as input to a Azure Stream Analytics job which simply passes the data through to Blob storage, recording it in JSON format. This path provides the least friction for the raw data, allowing it to be stored very quickly for auditing and due diligence purposes (it is good practice to have a complete record of the event data in case an issue arises subsequently). Operations staff are provided with a Hive query that they can run on an HDInsight cluster to take this data and generate a report listing each device and the number of events it generated.

2. The Stream Analytics job can also combine the data with reference information that maps device IDs to buildings. The reference information is held in Blob storage. The combined data is then processed to generate a rolling average of the temperatures recorded by all devices in a building during the last 5 seconds. The results are stored in an Azure SQL Database. This path adds value to the raw data by merging and summarizing it. Operations staff can query this summary data in the database to view the average temperature in each building.

3. The data can queried by using the Cold Storage Processor. This is a separate application that can be run by operations staff. The Cold Storage Processor reads data for events from the Event Hub and writes them to Blob storage (the Event Hub can also store a history of events for a configurable period.) The data in Blob storage can be subjected to detailed analysis and ad-hoc processing. Another Hive query is available to enable operations staff to summarize this data and generate a report.

The following diagram illustrates the relationships between the components that constitute the sample solution:

![The components that constitute the sample solution](media/overview-of-architecture-and-components/overview-of-architecture-and-components.png)

For detailed information on building and running the solution, see these [instructions](https://github.com/mspnp/iot-journey/blob/master/docs/building-and-deploying-the-sample-solution.md).

