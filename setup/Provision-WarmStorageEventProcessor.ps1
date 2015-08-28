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
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$EventHubSharedAccessPolicyName = "ManagePolicy",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$Location = "Central US"
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

    # Load modules.
    Load-Module -ModuleName Config -ModuleLocation .\modules
    Load-Module -ModuleName Utility -ModuleLocation .\modules
    Load-Module -ModuleName AzureARM -ModuleLocation .\modules
    Load-Module -ModuleName AzureStorage -ModuleLocation .\modules
    Load-Module -ModuleName AzureServiceBus -ModuleLocation .\modules


    if($AddAccount)
    {
        Add-AzureAccount
    }

    Select-AzureSubscription $SubscriptionName

    $StorageAccountInfo = New-ProvisionedStorageAccount -StorageAccountName $StorageAccountName `
                                                        -Location $Location

    $EventHubInfo = New-ProvisionedEventHub -SubscriptionName $SubscriptionName `
                                    -ServiceBusNamespace $ServiceBusNamespace `
                                    -EventHubName $EventHubName `
                                    -ConsumerGroupName $ConsumerGroupName `
                                    -EventHubSharedAccessPolicyName $EventHubSharedAccessPolicyName `
                                    -Location $Location `
                                    -PartitionCount 16 `
                                    -MessageRetentionInDays 7 `

    # Get Storage Account Key
    $storageAccountKey = Get-AzureStorageKey -StorageAccountName $StorageAccountName
    $storageAccountKeyPrimary = $storageAccountKey.Primary
    $RefdataContainerName = $ContainerName + "-refdata"
        
    $context = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $storageAccountKeyPrimary;

    New-StorageContainerIfNotExists -ContainerName $RefdataContainerName `
                                    -Context $context

    Set-BlobData -StorageAccountName $StorageAccountName `
                     -ContainerName $RefdataContainerName `
                     -BlobName "fabrikam/buildingdevice.json" `
                     -FilePath ".\data\fabrikam_buildingdevice.json"

    # Update settings

    $simulatorSettings = @{
        'Simulator.EventHubNamespace'= $EventHubInfo.EventHubNamespace;
        'Simulator.EventHubName' = $EventHubInfo.EventHubName;
        'Simulator.EventHubSasKeyName' = $EventHubInfo.EventHubSasKeyName;
        'Simulator.EventHubPrimaryKey' = $EventHubInfo.EventHubPrimaryKey;
        'Simulator.EventHubTokenLifetimeDays' = ($EventHubInfo.EventHubTokenLifetimeDays -as [string]);
    }

    Write-SettingsFile -configurationTemplateFile (Join-Path $PSScriptRoot -ChildPath "..\src\Simulator\ScenarioSimulator.ConsoleHost.Template.config") `
                       -configurationFile (Join-Path $PSScriptRoot -ChildPath "..\src\Simulator\ScenarioSimulator.ConsoleHost.config") `
                       -appSettings $simulatorSettings

    $serviceConfigFiles = Get-ChildItem -Include "ServiceConfiguration.Cloud.cscfg" -Path $(Join-Path $PSScriptRoot -ChildPath "..\src\Simulator") -Recurse
    Write-CloudSettingsFiles -serviceConfigFiles $serviceConfigFiles -appSettings $simulatorSettings
        
    $EventHubConnectionString = $EventHubInfo.ConnectionStringFix + ";TransportType=Amqp"
    $StorageAccountConnectionString = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}" -f $StorageAccountInfo.AccountName, $StorageAccountInfo.AccountKey

    $settings = @{
        'Warmstorage.CheckpointStorageAccount' = $StorageAccountConnectionString;
        'Warmstorage.EventHubConnectionString' = $EventHubConnectionString;
        'Warmstorage.EventHubName' = $EventHubInfo.EventHubName;
        'Warmstorage.ConsumerGroupName' = $ConsumerGroupName;
    }

    Write-SettingsFile -configurationTemplateFile (Join-Path $PSScriptRoot -ChildPath "..\src\AdhocExploration\DotnetEventProcessor\WarmStorage.EventProcessor.ConsoleHost.Template.config") `
                       -configurationFile (Join-Path $PSScriptRoot -ChildPath "..\src\AdhocExploration\DotnetEventProcessor\WarmStorage.EventProcessor.ConsoleHost.config") `
                       -appSettings $settings

    $serviceConfigFiles = Get-ChildItem -Include "ServiceConfiguration.Cloud.cscfg" -Path $(Join-Path $PSScriptRoot -ChildPath "..\src\AdhocExploration\DotnetEventProcessor") -Recurse
    Write-CloudSettingsFiles -serviceConfigFiles $serviceConfigFiles -appSettings $settings

    Write-Warning "This scenario requires Elastich Search installed to run which is not provisioned with this script."
    Write-Output "Provision Finished OK"
}
