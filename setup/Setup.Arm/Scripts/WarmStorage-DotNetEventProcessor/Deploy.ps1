# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

[CmdletBinding()]
Param
(
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SubscriptionName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][bool]$AddAccount =$True,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$StorageAccountName =$ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ContainerName = "warm-processor",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ServiceBusNamespace = $ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$EventHubName = "eventhub-iot",                  
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ConsumerGroupName  = "cg-elasticsearch", 
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$Location = "Central US"
)
PROCESS
{
    

    Write-Warning "This scenario requires Elastich Search installed to run which is not provisioned with this script."
    Write-Output "Provision Finished OK"
}
