#region Header

# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#
.SYNOPSIS
  

.DESCRIPTION
  

.PARAMETER SubscriptionName
    Name of the Azure Subscription.

.PARAMETER ApplicationName
    Name of the Application.

.PARAMETER StorageAccountName
    Name of the storage account name.

.PARAMETER ServiceBusNamespace
    Name of the Namespace tha will contain the EventHub instance.

.PARAMETER EventHubName
    Name of the EventHub that will receive events from the simulator.

.PARAMETER ConsumerGroupName
    Event Hub consumer group for blob storage.

.PARAMETER EventHubSharedAccessPolicyName
    Shared Access Policy

.PARAMETER ContainerName
    Name of the container use to store output from Event Hub.

.PARAMETER Location
    Location

.EXAMPLE
  .\Provision-ColdStorageEventProcessor.ps1 -SubscriptionName [YourAzureSubscriptionName] -ApplicationName [YourApplicationName]
#>

#endregion
[CmdletBinding()]
Param
(
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SubscriptionName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][bool]$AddAccount =$True,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$StorageAccountName =$ApplicationName + "sa",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ServiceBusNamespace = $ApplicationName + "sb",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$EventHubName = "eh01",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ConsumerGroupName  = "blobs",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ContainerName = "blobs-processor",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ResourceGroupName = "IoTJourney",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$DeploymentName = $ResourceGroupName + "Deployment",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$Location = "Central US"
)
PROCESS
{
    $ErrorActionPreference = "Stop"

    $ScriptsRootFolderPath = Join-Path $PSScriptRoot -ChildPath "..\"
    $ModulesFolderPath = Join-Path $PSScriptRoot -ChildPath "..\..\Modules"
    
    Push-Location $ScriptsRootFolderPath
        .\Init.ps1
    Pop-Location
    
    Load-Module -ModuleName Validation -ModuleLocation $ModulesFolderPath

    #Sanitize input
    $StorageAccountName = $StorageAccountName.ToLower()
    $ServiceBusNamespace = $ServiceBusNamespace.ToLower()

    # Validate input.
    Test-OnlyLettersAndNumbers "StorageAccountName" $StorageAccountName
    Test-OnlyLettersNumbersAndHyphens "ConsumerGroupName" $ConsumerGroupName
    Test-OnlyLettersNumbersHyphensPeriodsAndUnderscores "EventHubName" $EventHubName
    Test-OnlyLettersNumbersAndHyphens "ServiceBusNamespace" $ServiceBusNamespace
    Test-OnlyLettersNumbersAndHyphens "ContainerName" $ContainerName

    # Load modules.
    Load-Module -ModuleName Config -ModuleLocation $ModulesFolderPath
    Load-Module -ModuleName SettingsWriter -ModuleLocation $ModulesFolderPath
    Load-Module -ModuleName ResourceManager -ModuleLocation $ModulesFolderPath

    if($AddAccount)
    {
        Add-AzureAccount
    }

    Select-AzureSubscription $SubscriptionName
   
    #region Create Resources

    Invoke-InAzureResourceManagerMode ({
    
        New-AzureResourceGroupIfNotExists -ResourceGroupName $ResourceGroupName -Location $Location
    
        $resourcesInfo = New-AzureResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
                                         -Name "IoTJourneyDeployment" `
                                         -TemplateFile (Join-Path $PSScriptRoot -ChildPath ".\DeploymentTemplate.json") `
                                         -TemplateParameterObject @{ namespaceName = $ServiceBusNamespace; `
                                                                     eventHubName=$EventHubName; `
                                                                     consumerGroupName=$ConsumerGroupName; `
                                                                     storageAccountNameFix=$StorageAccountName
                                                                   }
    
    })

    #endregion

    #region Create EventHub's Authorization Rule

    $sbr = Get-AzureSBAuthorizationRule -Namespace $ServiceBusNamespace
    $namespaceManager = [Microsoft.ServiceBus.NamespaceManager]::CreateFromConnectionString($sbr.ConnectionString); 
    $eventHubDescription = Invoke-WithRetries ({ $namespaceManager.GetEventHub($eventHubName) })
    
    $rights = [Microsoft.ServiceBus.Messaging.AccessRights]::Listen, [Microsoft.ServiceBus.Messaging.AccessRights]::Send

    $accessRule = New-SharedAccessAuthorizationRule -Name "SendReceive" -Rights $rights
    $eventHubDescription.Authorization.Add($accessRule.Rule)
    $namespaceManager.UpdateEventHub($eventHubDescription)

    #endregion

    #region Update Settings

    $simulatorSettings = @{
        'Simulator.EventHubNamespace'= $serviceBusNamespace;
        'Simulator.EventHubName' = $eventHubName;
        'Simulator.EventHubSasKeyName' = $accessRule.PolicyName;
        'Simulator.EventHubPrimaryKey' = $accessRule.PolicyKey;
        'Simulator.EventHubTokenLifetimeDays' = ($eventHubDescription.MessageRetentionInDays -as [string]);
    }

    Write-SettingsFile -configurationTemplateFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\..\src\Simulator\ScenarioSimulator.ConsoleHost.Template.config") `
                       -configurationFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\..\src\Simulator\ScenarioSimulator.ConsoleHost.config") `
                       -appSettings $simulatorSettings
    
    $serviceConfigFiles = Get-ChildItem -Include "ServiceConfiguration.Cloud.cscfg" -Path $(Join-Path $PSScriptRoot -ChildPath "..\..\..\..\..\src\Simulator") -Recurse
    Write-CloudSettingsFiles -serviceConfigFiles $serviceConfigFiles -appSettings $simulatorSettings
    
    $storageAccountInfo = $null
    $storageAccountKey = $null
    Invoke-InAzureResourceManagerMode ({
        $storageAccountInfo = Get-AzureStorageAccount -StorageAccountName $StorageAccountName
        $storageAccountKey = Get-AzureStorageAccountKey -ResourceGroupName $ResourceGroupName -Name $StorageAccountName
    })

    $eventHubConnectionString = "Endpoint=sb://{0}.servicebus.windows.net/;SharedAccessKeyName={1};SharedAccessKey={2};TransportType=Amqp" -f $ServiceBusNamespace, $accessRule.PolicyName, $accessRule.PolicyKey
    $storageAccountConnectionString = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}" -f $storageAccountInfo.Name, $storageAccountKey.Key1

    $settings = @{
        'Coldstorage.CheckpointStorageAccount' = $storageAccountConnectionString;
        'Coldstorage.EventHubConnectionString' = $eventHubConnectionString;
        'Coldstorage.EventHubName' = $EventHubName;
        'Coldstorage.BlobWriterStorageAccount' = $storageAccountConnectionString;
        'Coldstorage.ContainerName' = $ContainerName;
        'Coldstorage.ConsumerGroupName' = $ConsumerGroupName;
        'Coldstorage.Tests.StorageConnectionString' = $storageAccountConnectionString;
    }

    Write-SettingsFile -configurationTemplateFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\..\src\LongTermStorage\DotnetEventProcessor\ColdStorage.EventProcessor.ConsoleHost.Template.config") `
                       -configurationFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\..\src\LongTermStorage\DotnetEventProcessor\ColdStorage.EventProcessor.ConsoleHost.config") `
                       -appSettings $settings

    $serviceConfigFiles = Get-ChildItem -Include "ServiceConfiguration.Cloud.cscfg" -Path $(Join-Path $PSScriptRoot -ChildPath "..\..\..\..\..\src\LongTermStorage\DotnetEventProcessor") -Recurse
    Write-CloudSettingsFiles -serviceConfigFiles $serviceConfigFiles -appSettings $settings

    #endregion
}