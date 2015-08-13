# Building and Running the IoT Long Term (Cold) Storage - Stream Analytics Implementation

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

	`.\Provision-ColdStorageAzureStreamAnalytics.ps1`

-  At the *SubscriptionName* prompt, enter the name of your Azure account subscription.

- At the *ApplicationName* prompt, enter a name that will be used for a service bus namespace, event hub instance, storage account, and stream analytics job.

- In the Sign in to Microsoft Azure dialog box, provide the credentials for your Azure account. 

- Wait while the script creates the Azure resources.

- To provision an HD Insight cluster, run the following command:

	`.\Provision-ColdStorageHDInsight.ps1`

-  At the *SubscriptionName* prompt, enter the name of your Azure account subscription.

- At the *ApplicationName* prompt, enter a name that will be used for your HDInsight cluster.

- In the Sign in to Microsoft Azure dialog box, provide the credentials for your Azure account. 

- In the Windows PowerShell credential request dialog box, enter a new password for the admin account for the HDInsight cluster to be created.

> **Note:** The password must be at least 10 characters long, contain a mixture up upper and lower case letters and numbers, and include at least one special character. If you fail to provide a sufficiently complex password, the provisioning script will fail to create the cluster and instead report a *PreClusterCreationValidationFailure*.

- Wait while the HDInsight cluster is provisioned. This can take several minutes.


### Verifying the Stream Analytics Configuration

To verify that the configuration was successful, perform the following steps:

- Using the Azure web portal, open the Stream Analytics page.

- On the Stream Analytics page, verify that the following Stream Analytics jobs have been created.

	- [ApplicationName]ToBlob

	- In the menu bar click Inputs.

	- On the Inputs page, click the "Test Connection" button for each of the inputs.

	- On the Outputs page, click the "Test Connection" button for each of the outputs.


## Running the Long Term (Cold) Storage - Stream Analytics Implementation

The following sections describe how to run the simulator and Stream Analytics cold storage job.

### Running the Simulator to Generate Events

Perform the following steps to run the simulator:

- Using Visual Studio, open the ScenarioSimulator solution and run the ScenarioSimualtor.ConsoleHost project.

- On the menu, select option 1 to provision the devices, then select option 2 (*Run NoErrorsExpected*). The simulator will generate events and echo their contents to the screen. The simulator should not report any exceptions.

- Allow the simulator to run for a few minutes and then press q.

- Press Enter to return to the menu.

### Start the Stream Analytics job:

- Using the Azure web portal, open the Stream Analytics page.

- Select [ApplicationName]ToBlob, and on the command bar click Start.

- In the Start Output dialog box, select Job Start Time and then click the tick button.

- Wait for the Stream Analytics job to start before continuing.


### Verifying the Raw Event Data Output by Stream Analytics

Perform the following steps to verify that events are being processed correctly by Stream Analytics:

- Using the Visual Studio Server Explorer window, connect to your Azure account.

- Expand the Storage node, expand the node that corresponds to your storage account, expand Blobs, and then double-click *blobs-asa* to display the contents pane.

- On the *blobs-asa* contents page, verify that the container has a folder named *fabrikam*.

- Double-click the *fabrikam* folder and verify that it contains one or more JSON files (files with random name but with the .json suffix).

- Double-click the most recent file to download and view the contents. Verify that the file contains a number of JSON formatted event records; there should be one record for each event that was generated when the simulator ran, although you probably didn't count them at the time! 

### Analyzing the Raw Event Data by Using a Hive Query

Perform the following steps to analyze the raw event data by using a Hive query:

- Move to the src\LongTermStorage\Validation\HDInsight folder.

- Run the following command:

	`.\hivequeryforstreamanalytics.ps1`

- At the *subscriptionName* prompt, enter the name of the subscription that owns the storage account holding the blob data used by the simulator.

- At the *storageAccountName* prompt, enter the name of the storage account: [ApplicationName]

-  At the *clusterName* prompt, enter the name of the HDInsight cluster: [ApplicationName]

-  In the Sign in to Windows Azure Powershell dialog box, provide the credentials for your Azure account.

- Verify that the message Successfully connected to cluster *cluster name* appears (where *cluster name* is the name of your HDInsight cluster), and then wait for the Hive query to complete. The results should appear on the standard output consisting of a series of pairs; the ID of a device and the number of events that were processed.