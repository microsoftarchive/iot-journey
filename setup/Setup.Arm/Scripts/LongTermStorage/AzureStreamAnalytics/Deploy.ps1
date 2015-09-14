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
    $LocalModulesFolderPath = Join-Path $PSScriptRoot -ChildPath "..\..\..\Modules"
    
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
    Load-Module -ModuleName RetryPolicy -ModuleLocation $LocalModulesFolderPath
    Load-Module -ModuleName ServiceBus -ModuleLocation $LocalModulesFolderPath
    
    if($AddAccount)
    {
        Add-AzureAccount
    }
    
    Select-AzureSubscription $SubscriptionName
   
    #region Create EventHub

    Invoke-InAzureResourceManagerMode ({
    
        New-AzureResourceGroupIfNotExists -ResourceGroupName $ResourceGroupName -Location $Location
        
        New-AzureResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
                                         -TemplateFile (Join-Path $PSScriptRoot -ChildPath ".\Templates\DeploymentTemplate_EventHub.json") `
                                         -TemplateParameterObject @{ namespaceName = $ServiceBusNamespace; eventHubName=$EventHubName; consumerGroupName=$ConsumerGroupName }
    
    })

    #endregion
    
    #region Create EventHub's Authorization Rule

    $namespaceAuthRule = Get-AzureSBAuthorizationRule -Namespace $serviceBusNamespace
    $namespaceManager = [Microsoft.ServiceBus.NamespaceManager]::CreateFromConnectionString($namespaceAuthRule.ConnectionString); 
    $eventHubDescription = Invoke-WithRetries ({ $namespaceManager.GetEventHub($EventHubName) })
    
    $rights = [Microsoft.ServiceBus.Messaging.AccessRights]::Listen,`
              [Microsoft.ServiceBus.Messaging.AccessRights]::Send

    $accessRule = New-SharedAccessAuthorizationRule -Name "SendReceive" -Rights $rights
    $eventHubDescription.Authorization.Add($accessRule.Rule)
    $namespaceManager.UpdateEventHub($eventHubDescription)
    
    #endregion

    #region Create ASA Job

    $StreamAnalyticsJobName = $ApplicationName+"ToBlob"
    Invoke-InAzureResourceManagerMode ({
        
        #create an ASA job instance.
        New-AzureResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
                                         -TemplateFile (Join-Path $PSScriptRoot -ChildPath ".\Templates\DeploymentTemplate_StreamAnalytics.json") `
                                         -TemplateParameterObject @{
                                            jobName = $StreamAnalyticsJobName;
                                            storageAccountNameFix = $StorageAccountName;
                                            consumerGroupName = $ConsumerGroupName;
                                            eventHubName = $EventHubName;
                                            serviceBusNamespace = $ServiceBusNamespace;
                                            sharedAccessPolicyName = $accessRule.PolicyName;
                                            sharedAccessPolicyKey = $accessRule.PolicyKey;
                                         }
    
    })

    #endregion
    
    #region Update Settings

    $simulatorSettings = @{
        'Simulator.EventHubNamespace'= $ServiceBusNamespace;
        'Simulator.EventHubName' = $EventHubName;
        'Simulator.EventHubSasKeyName' = $accessRule.PolicyName;
        'Simulator.EventHubPrimaryKey' = $accessRule.PolicyKey;
        'Simulator.EventHubTokenLifetimeDays' = ($EventHubDescription.MessageRetentionInDays -as [string]);
    }
    
    Write-SettingsFile -configurationTemplateFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\..\src\Simulator\ScenarioSimulator.ConsoleHost.Template.config") `
                       -configurationFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\..\src\Simulator\ScenarioSimulator.ConsoleHost.config") `
                       -appSettings $simulatorSettings

    #endregion
}