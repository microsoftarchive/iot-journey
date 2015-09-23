# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

[CmdletBinding()]
Param
(
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SubscriptionName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][bool]$AddAccount =$True,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$StorageAccountName =$ApplicationName + "sa",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ServiceBusNamespace = $ApplicationName + "sb",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$EventHubName = "eventhub-iot",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ConsumerGroupName  = "cg-blobs",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ContainerName = "blobs-processor",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ResourceGroupName = "IoTJourney",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$DeploymentName = "LongTermStorage-DotNetEventProcessor",
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
    Load-Module -ModuleName Storage -ModuleLocation $ModulesFolderPath

    if($AddAccount)
    {
        Add-AzureAccount
    }

    Select-AzureSubscription $SubscriptionName

    $Configuration = Get-Configuration
    Add-Library -LibraryName "Microsoft.ServiceBus.dll" -Location $Configuration.PackagesFolderPath

    $templatePath = (Join-Path $PSScriptRoot -ChildPath ".\azuredeploy.json")
    $primaryKey = [Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule]::GenerateRandomKey()
    $secondaryKey = [Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule]::GenerateRandomKey()

    Invoke-InAzureResourceManagerMode ({
    
        New-AzureResourceGroupIfNotExists -ResourceGroupName $ResourceGroupName -Location $Location
    
        $deployInfo = New-AzureResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
                                         -Name $DeploymentName `
                                         -TemplateFile $templatePath `
                                         -serviceBusNamespaceName $ServiceBusNamespace `
                                         -eventHubName $EventHubName `
                                         -consumerGroupName $ConsumerGroupName `
                                         -storageAccountNameFromTemplate $StorageAccountName `
                                         -containerName $ContainerName `
                                         -eventHubPrimaryKey $primaryKey `
                                         -eventHubSecondaryKey $secondaryKey

        #Create the container.
        $context = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $deployInfo.Outputs["storageAccountPrimaryKey"].Value
        New-StorageContainerIfNotExists -ContainerName $ContainerName -Context $context
    })
    
    Push-Location $PSScriptRoot

        .\Update-Settings.ps1 -SubscriptionName $SubscriptionName -ResourceGroupName $ResourceGroupName -DeploymentName $DeploymentName -AddAccount $false

    Pop-Location
}