# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

[CmdletBinding()]
Param
(
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SubscriptionName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][bool]$AddAccount = $True,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$StorageAccountName = "$($ApplicationName)sa",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ContainerName = "warm-processor",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ServiceBusNamespace = "$($ApplicationName)sb",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$EventHubName = "eventhub-iot",                  
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ConsumerGroupName  = "cg-elasticsearch",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ResourceGroupName = "IoTJourney",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$DeploymentName = "WarmStorage-DotNetEventProcessor",
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

    $storageAccountContext = $null

    Invoke-InAzureResourceManagerMode ({
    
        New-AzureResourceGroupIfNotExists -ResourceGroupName $ResourceGroupName -Location $Location
    
        $deployInfo = New-AzureResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
                                         -Name $DeploymentName `
                                         -TemplateFile $templatePath `
                                         -serviceBusNamespaceName $ServiceBusNamespace `
                                         -eventHubName $EventHubName `
                                         -consumerGroupName $ConsumerGroupName `
                                         -storageAccountNameFromTemplate $StorageAccountName `
                                         -eventHubPrimaryKey $primaryKey `
                                         -eventHubSecondaryKey $secondaryKey

        #Create the container.
        $storageAccountContext = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $deployInfo.Outputs["storageAccountPrimaryKey"].Value
        $referenceDataContainerName = "$($ContainerName)-refdata"
        New-StorageContainerIfNotExists -ContainerName $referenceDataContainerName -Context $storageAccountContext
    })

    $referenceDataFilePath = Join-Path $PSScriptRoot -ChildPath "..\..\Data\fabrikam_buildingdevice.json"

    Set-AzureStorageBlobContent -Blob "fabrikam/buildingdevice.json" `
                                -Container $referenceDataContainerName `
                                -File $referenceDataFilePath `
                                -Context $storageAccountContext `
                                -Force

    Push-Location $PSScriptRoot

        .\Update-Settings.ps1 -SubscriptionName $SubscriptionName -ResourceGroupName $ResourceGroupName -DeploymentName $DeploymentName -AddAccount $false

    Pop-Location
}