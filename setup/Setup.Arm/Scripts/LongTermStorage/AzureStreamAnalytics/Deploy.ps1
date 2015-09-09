# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#
.SYNOPSIS
  Deploy long term storage with Azure Stream Analytics example.
#>
[CmdletBinding()]
Param
(
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SubscriptionName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$StorageAccountName =$ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ServiceBusNamespace = $ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$EventHubName = "eventhub-iot",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ConsumerGroupName  = "cg-blobs-asa",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ResourceGroupName = "IoTJourney",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][bool]$AddAccount =$True,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$Location = "Central US"
)
PROCESS
{
    $ErrorActionPreference = "Stop"

    $SetupFolderPath = Join-Path $PSScriptRoot -ChildPath "..\..\..\..\"
    $ModulesFolderPath = Join-Path $PSScriptRoot -ChildPath "..\..\..\..\modules"
    
    Push-Location $SetupFolderPath
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
    
    Load-Module -ModuleName Config -ModuleLocation $ModulesFolderPath
    Load-Module -ModuleName Utility -ModuleLocation $ModulesFolderPath
    Load-Module -ModuleName AzureARM -ModuleLocation $ModulesFolderPath
    Load-Module -ModuleName AzureServiceBus -ModuleLocation $ModulesFolderPath
    
    if($AddAccount)
    {
        Add-AzureAccount
    }
    
    Select-AzureSubscription $SubscriptionName
    
    Invoke-InAzureResourceManagerMode ({
    
        New-AzureResourceGroupIfNotExists -ResourceGroupName $ResourceGroupName -Location $Location
    
        New-AzureResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
                                         -TemplateFile (Join-Path $PSScriptRoot -ChildPath ".\Templates\EventHub.json") `
                                         -TemplateParameterObject @{ namespaceName = $ServiceBusNamespace; eventHubName=$EventHubName; consumerGroupName=$ConsumerGroupName }
    
    })
    
    $sbr = Get-AzureSBAuthorizationRule -Namespace $ServiceBusNamespace
    $NamespaceManager = [Microsoft.ServiceBus.NamespaceManager]::CreateFromConnectionString($sbr.ConnectionString); 
    
    
    $EventHubDescription = $NamespaceManager.GetEventHub($EventHubName)
    
    #TODO: this is always regenerating the key. A parameter indicating if we want to do this explicitly may be better.
    $PolicyKey = [Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule]::GenerateRandomKey()
    $Rights = [Microsoft.ServiceBus.Messaging.AccessRights]::Listen, [Microsoft.ServiceBus.Messaging.AccessRights]::Send
    $RightsColl = New-Object -TypeName System.Collections.Generic.List[Microsoft.ServiceBus.Messaging.AccessRights] (,[Microsoft.ServiceBus.Messaging.AccessRights[]]$Rights)
    $PolicyName = "SendReceive"
    $AccessRule = New-Object -TypeName  Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule -ArgumentList $PolicyName, $PolicyKey, $RightsColl
    $EventHubDescription.Authorization.Add($AccessRule)
    
    $NamespaceManager.UpdateEventHub($EventHubDescription)
    
    $StreamAnalyticsJobName = $ApplicationName+"ToBlob"
    Invoke-InAzureResourceManagerMode ({
    
        New-AzureResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
                                         -TemplateFile (Join-Path $PSScriptRoot -ChildPath ".\Templates\StreamAnalytics.json") `
                                         -TemplateParameterObject @{
                                            jobName = $StreamAnalyticsJobName;
                                            storageAccountNameFix = $StorageAccountName;
                                            consumerGroupName = $ConsumerGroupName;
                                            eventHubName = $EventHubName;
                                            serviceBusNamespace = $ServiceBusNamespace;
                                            sharedAccessPolicyName = $PolicyName;
                                            sharedAccessPolicyKey = $PolicyKey;
                                         }
    
    })
    
    # Update settings
    $simulatorSettings = @{
        'Simulator.EventHubNamespace'= $ServiceBusNamespace;
        'Simulator.EventHubName' = $EventHubName;
        'Simulator.EventHubSasKeyName' = $PolicyName;
        'Simulator.EventHubPrimaryKey' = $PolicyKey;
        'Simulator.EventHubTokenLifetimeDays' = ($EventHubDescription.MessageRetentionInDays -as [string]);
    }
    
    Write-SettingsFile -configurationTemplateFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\..\src\Simulator\ScenarioSimulator.ConsoleHost.Template.config") `
                       -configurationFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\..\src\Simulator\ScenarioSimulator.ConsoleHost.config") `
                       -appSettings $simulatorSettings
}