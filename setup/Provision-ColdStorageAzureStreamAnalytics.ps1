<#
.SYNOPSIS
  

.DESCRIPTION
  

.PARAMETER SubscriptionName
    Name of the Azure Subscription.

.PARAMETER StorageAccountName
    Name of the storage account name.
     
.PARAMETER ContainerName
    Name of the container use to store output from the Stream Analytics job.

.PARAMETER BlobsConsumerGroupName
    Stream Analytics consumer group for blob storage.

.PARAMETER EventHubName
    Name of the EventHub that will receive events from the simulator.

.PARAMETER EventHubSharedAccessPolicyName
    Shared Access Policy

.PARAMETER Location
    Location

.PARAMETER ResourceGroupPrefix
    Prefix that will be combined with Location to produce the ResourceGroupName

.PARAMETER ServiceBusNamespace
    Name of the Namespace tha will contain the EventHub instance.

.PARAMETER StreamAnalyticsBlobsJobName
    Name of the Stream Analytics Job name used to output to blob storage.
    
.EXAMPLE
  .\Provision-ColdStorageAzureStreamAnalytics.ps1 -SubscriptionName [YourAzureSubscriptionName] -StorageAccountName [YourStorageAccountName] -Verbose
#>
[CmdletBinding()]
Param
(
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SubscriptionName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ApplicationName,
	[ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][bool]$AddAccount = $True,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$StorageAccountName =$ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ContainerName = "blobs-asa",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ConsumerGroupName  = "cg-blobs-asa",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$EventHubName = "eventhub-iot",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$EventHubSharedAccessPolicyName = "ManagePolicy",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$Location = "Central US",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ResourceGroupPrefix = $ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ServiceBusNamespace = $ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$StreamAnalyticsJobName = $ApplicationName+"ToBlob",

    
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][ValidateScript({ Test-FileName "JobDefinitionPath" $_})]
    [String]$JobDefinitionPath = "..\src\LongTermStorage\AzureStreamAnalytics\StreamAnalyticsJobDefinitionColdStorage.json"

)
PROCESS
{
    .\Init.ps1

    Load-Module -ModuleName Validation -ModuleLocation .\modules

    # Validate input.
    Test-OnlyLettersAndNumbers "StorageAccountName" $StorageAccountName
    Test-OnlyLettersNumbersAndHyphens "ConsumerGroupName" $ConsumerGroupName
    Test-OnlyLettersNumbersHyphensPeriodsAndUnderscores "EventHubName" $EventHubName
    Test-OnlyLettersNumbersAndHyphens "ServiceBusNamespace" $ServiceBusNamespace
    Test-OnlyLettersNumbersAndHyphens "ContainerName" $ContainerName

    # Load modules.
    Load-Module -ModuleName Config -ModuleLocation .\modules
    Load-Module -ModuleName AzureARM -ModuleLocation .\modules
    Load-Module -ModuleName AzureStorage -ModuleLocation .\modules
    Load-Module -ModuleName AzureServiceBus -ModuleLocation .\modules
    Load-Module -ModuleName AzureStreamAnalytics -ModuleLocation .\modules

    if($AddAccount)
    {
        Add-AzureAccount
    }

    Select-AzureSubscription $SubscriptionName

    Provision-StorageAccount -StorageAccountName $StorageAccountName `
                                             -ContainerName $ContainerName `
                                             -Location $Location
        
    Provision-EventHub -SubscriptionName $SubscriptionName `
                                    -ServiceBusNamespace $ServiceBusNamespace `
                                    -EventHubName $EventHubName `
                                    -ConsumerGroupName $ConsumerGroupName `
                                    -EventHubSharedAccessPolicyName $EventHubSharedAccessPolicyName `
                                    -Location $Location `
                                    -PartitionCount 16 `
                                    -MessageRetentionInDays 7 `
        
    $ResourceGroupName = $ResourceGroupPrefix + "-" + $Location.Replace(" ","-")

    $EventHubSharedAccessPolicyKey = Get-EventHubSharedAccessPolicyKey -ServiceBusNamespace $ServiceBusNamespace `
                                                                           -EventHubName $EventHubName `
                                                                           -EventHubSharedAccessPolicyName $EventHubSharedAccessPolicyName

    # Get Storage Account Key
    $storageAccountKey = Get-AzureStorageKey -StorageAccountName $StorageAccountName
    $storageAccountKeyPrimary = $storageAccountKey.Primary

	# Create Job Definition
    [string]$JobDefinitionText = (Get-Content -LiteralPath (Join-Path $PSScriptRoot -ChildPath $JobDefinitionPath)).
                                    Replace("_StreamAnalyticsJobName",$StreamAnalyticsJobName).
                                    Replace("_Location",$Location).
                                    Replace("_ConsumerGroupName",$ConsumerGroupName).
                                    Replace("_EventHubName",$EventHubName).
                                    Replace("_ServiceBusNamespace",$ServiceBusNamespace).
                                    Replace("_EventHubSharedAccessPolicyName",$EventHubSharedAccessPolicyName).
                                    Replace("_EventHubSharedAccessPolicyKey",$EventHubSharedAccessPolicyKey).
                                    Replace("_AccountName",$StorageAccountName).
                                    Replace("_AccountKey",$storageAccountKeyPrimary).
                                    Replace("_Container",$ContainerName)

    Provision-StreamAnalyticsJob -ServiceBusNamespace $ServiceBusNamespace `
                                            -EventHubName $EventHubName `
                                            -EventHubSharedAccessPolicyName $EventHubSharedAccessPolicyName `
                                            -StorageAccountName $StorageAccountName `
                                            -ContainerName $ContainerName `
                                            -ResourceGroupName $ResourceGroupName `
                                            -Location $Location `
                                            -JobDefinitionText $JobDefinitionText
    
    Write-Output "Provision Finished OK"                                               
}