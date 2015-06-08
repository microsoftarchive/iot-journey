![Microsoft patterns & practices](http://pnp.azurewebsites.net/images/pnp-logo.png)
# Steps
1. Run Porvision-All

1. in Windowsazure portal, open the stream analytics job provisioned, and test connections for input01, input02, output01, and output02

1. Upload fabrikam_buildingdevice.json to the container02refdata of the stroage account and rename it as as fabrikam/buildingdevice.json

1. Update mysettings.config file Simulator.EventHubConnectionString and Simulator.EventHubPath with the vaule in the service bus just provisioned.

1. Start running the simulator

1. Start the Stream Analytics Job
