<#
.SYNOPSIS
  

.DESCRIPTION
  

.PARAMETER SubscriptionName
    Name of the Azure Subscription.

.PARAMETER ApplicationName
    Name of the Application.
    
.EXAMPLE
  .\Provision-All.ps1 -SubscriptionName [YourAzureSubscriptionName] -ApplicationName [YourApplicationName]
#>
[CmdletBinding()]
Param
(
	[ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SubscriptionName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ApplicationName
)
PROCESS
{
    Add-AzureAccount

    .\Provision-ColdStorageEventProcessor.ps1 -SubscriptionName $SubscriptionName -ApplicationName $ApplicationName -AddAccount $False

    .\Provision-WarmStorageEventProcessor.ps1 -SubscriptionName $SubscriptionName -ApplicationName $ApplicationName -AddAccount $False

    .\Provision-ColdStorageAzureStreamAnalytics.ps1 -SubscriptionName $SubscriptionName -ApplicationName $ApplicationName -AddAccount $False

    .\Provision-ColdStorageHDInsight.ps1 -SubscriptionName $SubscriptionName -ApplicationName $ApplicationName -AddAccount $False
}
