# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

[CmdletBinding()]
Param
(
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SubscriptionName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ResourceGroupName = "IoTJourney",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$DeploymentName = "LongTermStorage-AzureStreamAnalytics",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][bool]$AddAccount = $true
)
PROCESS
{
    $ErrorActionPreference = "Stop"

    $ScriptsRootFolderPath = Join-Path $PSScriptRoot -ChildPath "..\"
    $ModulesFolderPath = Join-Path $PSScriptRoot -ChildPath "..\..\Modules"
    
    Push-Location $ScriptsRootFolderPath
        .\Init.ps1
    Pop-Location
    
    Load-Module -ModuleName ResourceManager -ModuleLocation $ModulesFolderPath

    if($AddAccount)
    {
        Add-AzureAccount
    }
    
    Select-AzureSubscription $SubscriptionName

    $deploymentInfo = $null
    Invoke-InAzureResourceManagerMode ({
        $deploymentInfo = Get-AzureResourceGroupDeployment -ResourceGroupName $ResourceGroupName  -Name $DeploymentName
    })

    #simulator settings
    $simulatorSettings = @{
        'Simulator.EventHubNamespace'= $deploymentInfo.Outputs["serviceBusNamespaceName"].Value;
        'Simulator.EventHubName' = $deploymentInfo.Outputs["eventHubName"].Value;
        'Simulator.EventHubSasKeyName' = $deploymentInfo.Outputs["sharedAccessPolicyName"].Value;
        'Simulator.EventHubPrimaryKey' = $deploymentInfo.Outputs["sharedAccessPolicyPrimaryKey"].Value;
        'Simulator.EventHubTokenLifetimeDays' = ($deploymentInfo.Outputs["messageRetentionInDays"].Value -as [string]);
    }
    
    Write-SettingsFile -configurationTemplateFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\src\Simulator\ScenarioSimulator.ConsoleHost.Template.config") `
                       -configurationFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\src\Simulator\ScenarioSimulator.ConsoleHost.config") `
                       -appSettings $simulatorSettings
    
    #cloud services settings
    $serviceConfigFiles = Get-ChildItem -Include "ServiceConfiguration.Cloud.cscfg" -Path $(Join-Path $PSScriptRoot -ChildPath "..\..\..\..\src\Simulator") -Recurse
    Write-CloudSettingsFiles -serviceConfigFiles $serviceConfigFiles -appSettings $simulatorSettings

    $eventHubAmqpConnectionString = $deploymentInfo.Outputs["eventHubAmqpConnectionString"].Value
    $storageAccountConnectionString = $deploymentInfo.Outputs["storageAccountConnectionString"].Value

    $settings = @{
        'Coldstorage.CheckpointStorageAccount' = $storageAccountConnectionString;
        'Coldstorage.EventHubConnectionString' = $eventHubAmqpConnectionString;
        'Coldstorage.EventHubName' = $deploymentInfo.Outputs["eventHubName"].Value;
        'Coldstorage.BlobWriterStorageAccount' = $storageAccountConnectionString;
        'Coldstorage.ContainerName' = $deploymentInfo.Outputs["containerName"].Value;
        'Coldstorage.ConsumerGroupName' = $deploymentInfo.Outputs["consumerGroupName"].Value;
        'Coldstorage.Tests.StorageConnectionString' = $storageAccountConnectionString;
    }

    Write-SettingsFile -configurationTemplateFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\src\LongTermStorage\DotnetEventProcessor\ColdStorage.EventProcessor.ConsoleHost.Template.config") `
                       -configurationFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\src\LongTermStorage\DotnetEventProcessor\ColdStorage.EventProcessor.ConsoleHost.config") `
                       -appSettings $settings

    $serviceConfigFiles = Get-ChildItem -Include "ServiceConfiguration.Cloud.cscfg" -Path $(Join-Path $PSScriptRoot -ChildPath "..\..\..\..\src\LongTermStorage\DotnetEventProcessor") -Recurse
    Write-CloudSettingsFiles -serviceConfigFiles $serviceConfigFiles -appSettings $settings
}