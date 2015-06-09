![Microsoft patterns & practices](http://pnp.azurewebsites.net/images/pnp-logo.png)
# Steps

> Note: These steps are a brief overview. For detailed instructions on building and provisioning the sample solution, see ![Building and Running the IoT Sample Solution](building-and-deploying-the-sample-solution.md)

1. Build the solution to download the nuget files (the build process will report that the file mysettings.config is missing).

1. Run Provision-All using Azure PowerShell

1. In the Azure portal, open the stream analytics job fabrikamstreamjob01, and test the connections for input01, input02, output01, and output02

1. Upload fabrikam_buildingdevice.json to the container01refdata of the storage account and rename it as as fabrikam/buildingdevice.json

1. Update the mysettings.config file - set the Simulator.EventHubConnectionString and Simulator.EventHubPath with the values for the newly provisioned event hub.

1. Start the Stream Analytics Job

1. Start running the simulator


Notes:

- All passwords require an uppercase letter, lowercase letter, a number and a special character. The password for the HDInsight cluster must be at least 10 characters long.

- Check to make sure you have sufficient resources to create the Hadoop cluster.

- Set all blob containers to be publicly accessible by the Hadoop cluster. 
