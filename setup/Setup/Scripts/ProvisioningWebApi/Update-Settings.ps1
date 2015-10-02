# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

[CmdletBinding()]
Param
(
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SubscriptionName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ResourceGroupName = "IoTJourney",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$DeploymentName = "ProvisioningWebApi",
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

    $settings = @{
        'EventHubNamespace'= $deploymentInfo.Outputs["serviceBusNamespaceName"].Value;
        'EventHubName' = $deploymentInfo.Outputs["eventHubName"].Value;
        'EventHubSasKeyName' = $deploymentInfo.Outputs["sharedAccessPolicyName"].Value;
        'EventHubPrimaryKey' = $deploymentInfo.Outputs["sharedAccessPolicyPrimaryKey"].Value;
        'EventHubConnectionString' = $deploymentInfo.Outputs["eventHubAmqpConnectionString"].Value;
        'StorageConnectionString' = $deploymentInfo.Outputs["storageAccountConnectionString"].Value;
    }

    Write-SettingsFile -configurationTemplateFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\src\DeviceProvisioning\ProvisioningWebApi\ProvisioningWebApi\settings.template.config") `
                       -configurationFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\src\DeviceProvisioning\ProvisioningWebApi\ProvisioningWebApi\settings.config") `
                       -appSettings $settings
}