# Building and Deploying the IoT Example System

This document describes how to build and deploy the IoT example system. 

You can download the code for the solution [here](https://github.com/mspnp/iot-journey/tree/master/src).

You should also download the [provisioning scripts](https://github.com/mspnp/iot-journey/tree/master/provision) that create the Azure assets required by the solution.

The solution comprises the following projects:

- ScenarioSimulator. This project *TBD* ...

- Device.Events. This project *TBD* ...

- Core. This project *TBD* ...

- ScenarioSimulator.ConsoleHost. This project *TBD* ...

- ScenarioSimulator.Tests and Core.Tests (in the UnitTests folder). These projects *TBD* ...

## Prerequisites

To use the sample solution, you must have an Azure account with a subscription. You should also have installed the following software on your computer:

- [Visual Studio](https://www.visualstudio.com/)

- [Azure SDK for .NET (Visual Studio tools)](http://azure.microsoft.com/downloads/)

- [Azure Powershell](https://azure.microsoft.com/documentation/articles/powershell-install-configure/)

## Overview

To build and deploy the system, perform the following steps:

1. Download and install the NuGet packages and assemblies required by the solution.

2. Create the required Azure assets and services.

3. Create the configuration file for the solution with your Azure account information.

4. Configure Stream Analytics.

5. Configure HDInsight.

6. Test and verify the solution.

The following sections describe these steps in more detail.

### Downloading and Installing the Required Assemblies

- Using Visual Studio, open the IoTJourney solution.

- Rebuild the solution. This action downloads the various NuGet packages referenced by the solution and installs the appropriate assemblies.

> **Note:** The rebuild process should report 1 error: *Before building this project, please copy mysettings-template.config to mysettings.config ...* You will do this in a later step. If rebuild process displays any other errors then clean the solution and rebuild it again.

### Creating the Required Azure Assets and Services

- Using the Azure portal, manually create the following items:

	- [A Service Bus namespace](https://msdn.microsoft.com/library/azure/hh690931.aspx). The namespace should be created in the Central US region. Set the namespace type to Notification Hub.

	- [A storage account](https://azure.microsoft.com/documentation/articles/storage-create-storage-account/). Set the location/affinity group to Central US.

- Start Azure PowerShell as administrator.

> **Note:** Windows Powershell must be configured to run scripts. You can do this by running the following PowerShell command before continuing:
> 
> `set-executionpolicy unrestricted`

- Move to the folder containing the provisioning scripts.

- Run the following command:

	`.\Provision-All.ps1`

- At the *ServiceBusNamespace* prompt, enter the name of the Service Bus namespace you created in step 1.

- At the *StorageAccountName* prompt, enter the name of the storage account you created in step 1.

- At the *StreamAnalyticsJobName* prompt, enter a name for the Stream Analytics job to be created (the script will create this item).

-  At the *SubscriptionName* prompt, enter the name of the subscription that you are using with your Azure account (this subscription should be the owner of the Service Bus namespace and the storage account).

-  In the Sign in to Windows Azure Powershell dialog box, provide the credentials for your Azure account.

### Creating the Configuration File

- Using File Explorer create of a copy of the file named *mysettings-template.config* in the RunFromConsole folder as *mysettings.config*

- In Visual Studio, open the *mysettings.config* file in the ScenarioSimulator.ConsoleHost project. 

- Using the Azure portal, find the connection string for the eventhub01 event hub in the Service Bus namespace that you created earlier.

- In Visual Studio, set the value of the Simulator.EventHubConnectionString key to the connection string for the eventhub01 event hub.

- Rebuild the solution. It should build successfully.

### Configuring Stream Analytics

### Configuring HDInsight

## Testing and Verifying the Solution

### Running the Simulator

### Using the Unit Tests

### Validating with Stream Analytics

### Validating by Using Hive Queries

### Validating by Inspecting Blob Data
