# Building and Running the IoT Warm Storage - Ad Hoc Exploration Implementation

This Scenario Simulator simulates a large number of devices sending environmental readings for apartments in a smart building. 

The Scenario Simulator enables you to test the following:

1. All devices sending readings that are within the expected range. 

2. Some devices send readings indicating that the temperature is excessive (over 30 degrees Celsius).

You can download the code for the Scenario Simulator [here](../src/Simulator).

You should also download the [setup scripts](../setup) that create the Azure assets required by the Scenario Simulator.

## Prerequisites

You must have an Azure account with a subscription. You should also have installed the following software on your computer:

- [Visual Studio](https://www.visualstudio.com/)

- [Azure SDK for .NET (Visual Studio tools)](http://azure.microsoft.com/downloads/)

- [Azure Powershell](https://azure.microsoft.com/documentation/articles/powershell-install-configure/)

## Building the Scenario Simulator Solution

To build and provision the system, perform the following steps:

1. Download and install the NuGet packages and assemblies required by the solution.

2. Create the required Azure assets and services.

3. Verify the Stream Analytics configuration.

4. Build the solution.

The following sections describe these steps in more detail.

### Downloading and Installing the Required Assemblies

- Using Visual Studio, open the ScenarioSimulator solution.

- Rebuild the solution. This action downloads the various NuGet packages referenced by the solution and installs the appropriate assemblies.

> **Note:** The rebuild process should report an error: *Before building this project, please copy ScenarioSimulator.ConsoleHost.Template.config to ScenarioSimulator.ConsoleHost.config ...* You will do this in a later step. If the rebuild process displays any other errors then clean the solution and rebuild it again.

### Creating the Required Azure Assets and Services

> **Note:** Windows Powershell must be configured to run scripts. You can do this by running the following PowerShell command before continuing:
> 
> `set-executionpolicy unrestricted`

- Move to the folder containing the setup scripts.

- Run the following command:

	`.\Provision-WarmStorageEventProcessor.ps1`

-  At the *SubscriptionName* prompt, enter the name of your Azure account subscription.

- At the *ApplicationName* prompt, enter a name that will be used for a service bus namespace, event hub instance, and storage account.

- In the Sign in to Microsoft Azure dialog box, provide the credentials for your Azure account. 

- Wait while the script creates the Azure resources.


## Running the Warm Storage - Ad Hoc Exploration Implementation

The following sections describe how to run the simulator and Warm Storage Event Processor.

### Running the Simulator to Generate Events

Perform the following steps to run the simulator:

- Using Visual Studio, open the ScenarioSimulator solution and run the ScenarioSimualtor.ConsoleHost project.

- On the menu, select option 1 to provision the devices, then select option 2 (*Run NoErrorsExpected*). The simulator will generate events and echo their contents to the screen. The simulator should not report any exceptions.

- Allow the simulator to run for a few minutes and then press q.

- Press Enter to return to the menu.

### Running the Warm Storage Event Processor

Perform the following steps to run the warm storage event processor. 

- Using Visual Studio, open the WarmStorage.EventProcessor solution and run the WarmStorage.EventProcessor.ConsoleHost project.

- On the menu, select option 1 (*Provision Resources*). The warm storage event processor will check that the resources it requires are available, or create them if necessary.

- Press Enter to return to the menu.

- On the menu, select option 2 (*Run Warm Storage Consumer*).

- Allow the Warm Storage Conumer to run.

### Running Elasticsearch

- Download [Elasticsearch](https://www.elastic.co/products/elasticsearch).

- Start your Elasticsearch node.

- Once your Elasticsearch node is running, the warm storage event processor should send it event data using the address: http://localhost:9200

### Running Kibana

- Download [Kibana](https://www.elastic.co/products/kibana).

- Start your Kibana instance.

- Once your Kibana instance is running, you should be able to see event data in your local [Kibana instance](http://localhost:5601). 

- Kibana may ask for the index pattern. The warm storage event processor creates Elasticsearch indexes using the pattern: "iot-*"

- You may need to configure Kibana to show you the last hours worth of data.


