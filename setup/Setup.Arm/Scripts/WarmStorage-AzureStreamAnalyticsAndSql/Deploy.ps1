# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

[CmdletBinding()]
Param
(
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SubscriptionName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$StorageAccountName = "$($ApplicationName)sa",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ServiceBusNamespaceName = "$($ApplicationName)sb",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$EventHubName = "eventhub-iot",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ConsumerGroupName = "cg-sql-asa",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$SqlServerName = "$($ApplicationName)sql",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$SqlServerAdminLogin = "dbuser",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$SqlDatabaseName = "fabrikamdb",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SqlServerAdminLoginPassword,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ResourceGroupName = "IoTJourney",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$DeploymentName = "$($ResourceGroupName)Deployment",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$Location = "Central US",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][bool]$AddAccount = $True
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
    $ServiceBusNamespaceName = $ServiceBusNamespaceName.ToLower()
    $SqlServerName = $SqlServerName.ToLower()

    # Validate input.
    Test-OnlyLettersAndNumbers "StorageAccountName" $StorageAccountName
    Test-OnlyLettersNumbersAndHyphens "ConsumerGroupName" $ConsumerGroupName
    Test-OnlyLettersNumbersHyphensPeriodsAndUnderscores "EventHubName" $EventHubName
    Test-OnlyLettersNumbersAndHyphens "ServiceBusNamespace" $ServiceBusNamespaceName
    
    Load-Module -ModuleName Config -ModuleLocation $ModulesFolderPath
    Load-Module -ModuleName SettingsWriter -ModuleLocation $ModulesFolderPath
    Load-Module -ModuleName ResourceManager -ModuleLocation $ModulesFolderPath
    Load-Module -ModuleName Storage -ModuleLocation $ModulesFolderPath
    
    if($AddAccount)
    {
        Add-AzureAccount
    }
    
    Select-AzureSubscription $SubscriptionName

    #region Create Resources

    $Configuration = Get-Configuration
    Add-Library -LibraryName "Microsoft.ServiceBus.dll" -Location $Configuration.PackagesFolderPath

    $templatePath = (Join-Path $PSScriptRoot -ChildPath ".\azuredeploy.json")
    $streamAnalyticsJobName = "$($ApplicationName)ToSql"
    $primaryKey = [Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule]::GenerateRandomKey()
    $secondaryKey = [Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule]::GenerateRandomKey()

    $storageAccountContext = $null

    Invoke-InAzureResourceManagerMode ({
    
        New-AzureResourceGroupIfNotExists -ResourceGroupName $ResourceGroupName -Location $Location
        
        $referenceDataContainerName = "warm-asa-refdata"

        $deployInfo = New-AzureResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
                                                       -Name $DeploymentName `
                                                       -TemplateFile $templatePath `
                                                       -asaJobName $streamAnalyticsJobName `
                                                       -storageAccountNameFromTemplate $StorageAccountName `
                                                       -serviceBusNamespaceName $ServiceBusNamespaceName `
                                                       -eventHubName $EventHubName `
                                                       -consumerGroupName $ConsumerGroupName `
                                                       -eventHubPrimaryKey $primaryKey `
                                                       -eventHubSecondaryKey $secondaryKey `
                                                       -sqlServerName $SqlServerName `
                                                       -sqlServerAdminLogin $SqlServerAdminLogin `
                                                       -sqlServerAdminLoginPassword $SqlServerAdminLoginPassword `
                                                       -sqlDatabaseName $SqlDatabaseName `
                                                       -sqlDatabaseUser $SqlServerAdminLogin `
                                                       -referenceDataContainerName $referenceDataContainerName

        #Create the container.
        $storageAccountContext = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $deployInfo.Outputs["storageAccountPrimaryKey"].Value
        New-StorageContainerIfNotExists -ContainerName $referenceDataContainerName -Context $storageAccountContext
    })

    $referenceDataFilePath = Join-Path $PSScriptRoot -ChildPath "..\..\Data\fabrikam_buildingdevice.json"

    Set-AzureStorageBlobContent -Blob "fabrikam/buildingdevice.json" `
                                -Container $referenceDataContainerName `
                                -File $referenceDataFilePath `
                                -Context $storageAccountContext `
                                -Force

    $qualifiedSqlServerName = $SqlServerName + ".database.windows.net"

    $schemaFilePath = Join-Path $PSScriptRoot -ChildPath "..\..\Data\CreateSqlDatabase_Schema.sql"

    Invoke-Sqlcmd -InputFile $schemaFilePath `
                  -ServerInstance $qualifiedSqlServerName `
                  -Database $SqlDatabaseName `
                  -Username $SqlServerAdminLogin `
                  -Password $SqlServerAdminLoginPassword

    #endregion

    #simulator settings
    $simulatorSettings = @{
        'Simulator.EventHubNamespace'= $deployInfo.Outputs["serviceBusNamespaceName"].Value;
        'Simulator.EventHubName' = $deployInfo.Outputs["eventHubName"].Value;
        'Simulator.EventHubSasKeyName' = $deployInfo.Outputs["sharedAccessPolicyName"].Value;
        'Simulator.EventHubPrimaryKey' = $deployInfo.Outputs["sharedAccessPolicyPrimaryKey"].Value;
        'Simulator.EventHubTokenLifetimeDays' = ($deployInfo.Outputs["messageRetentionInDays"].Value -as [string]);
    }
    
    Write-SettingsFile -configurationTemplateFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\src\Simulator\ScenarioSimulator.ConsoleHost.Template.config") `
                       -configurationFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\src\Simulator\ScenarioSimulator.ConsoleHost.config") `
                       -appSettings $simulatorSettings
}