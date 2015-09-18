# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

[CmdletBinding()]
Param
(
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SubscriptionName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$StorageAccountName = "$($ApplicationName)sa",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ContainerName = "warm-asa",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ServiceBusNamespaceName = "$($ApplicationName)sb",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$EventHubName = "eventhub-iot",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ConsumerGroupName = "cg-sql-asa",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$SqlServerName = "$($ApplicationName)sql",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$SqlServerAdminLogin = "dbuser",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$SqlDatabaseName = "fabrikamdb",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][securestring]$SqlServerAdminLoginPassword,
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
    Test-OnlyLettersNumbersAndHyphens "ContainerName" $ContainerName
    
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

    Invoke-InAzureResourceManagerMode ({
    
        New-AzureResourceGroupIfNotExists -ResourceGroupName $ResourceGroupName -Location $Location
        
        $deployInfo = New-AzureResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
                                                       -Name $DeploymentName `
                                                       -TemplateFile $templatePath `
                                                       -asaJobName $streamAnalyticsJobName `
                                                       -containerName $ContainerName `
                                                       -storageAccountNameFromTemplate $StorageAccountName `
                                                       -serviceBusNamespaceName $ServiceBusNamespaceName `
                                                       -eventHubName $EventHubName `
                                                       -consumerGroupName $ConsumerGroupName `
                                                       -sharedAccessPolicyPrimaryKey $primaryKey `
                                                       -sharedAccessPolicySecondaryKey $secondaryKey `
                                                       -sqlServerName $SqlServerName `
                                                       -sqlServerAdminLogin $SqlServerAdminLogin `
                                                       -sqlServerAdminLoginPassword $SqlServerAdminLoginPassword `
                                                       -sqlDatabaseName $SqlDatabaseName `
                                                       -sqlDatabaseUser $SqlServerAdminLogin `
                                                       -referenceDataContainerName "$($ContainerName)-refdata"

        #Create the container.
        #$context = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $deployInfo.Outputs["storageAccountPrimaryKey"].Value
        #New-StorageContainerIfNotExists -ContainerName $ContainerName -Context $context
    })

    #endregion

    <#

    New-ProvisionedStorageAccount -StorageAccountName $StorageAccountName `
                                  -ContainerName $ContainerName `
                                  -Location $Location
        
    $EventHubInfo = New-ProvisionedEventHub -SubscriptionName $SubscriptionName `
                                    -ServiceBusNamespace $ServiceBusNamespaceName `
                                    -EventHubName $EventHubName `
                                    -ConsumerGroupName $ConsumerGroupName `
                                    -EventHubSharedAccessPolicyName $EventHubSharedAccessPolicyName `
                                    -Location $Location `
                                    -PartitionCount 16 `
                                    -MessageRetentionInDays 7 `

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

    $ResourceGroupName = $ResourceGroupPrefix + "-" + $Location.Replace(" ","-")

    Provision-SqlDatabase -SqlServerName $SqlServerName `
                                        -SqlServerAdminLogin $SqlServerAdminLogin `
                                        -SqlServerAdminPassword $SqlServerAdminPassword `
                                        -SqlDatabaseName $SqlDatabaseName `
                                        -ResourceGroupName $ResourceGroupName `
                                        -Location $Location



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


        $EventHubSharedAccessPolicyKey = Get-EventHubSharedAccessPolicyKey -ServiceBusNamespace $ServiceBusNamespaceName `
                                                                           -EventHubName $EventHubName `
                                                                           -EventHubSharedAccessPolicyName $EventHubSharedAccessPolicyName

        # Get Storage Account Key
        $storageAccountKey = Get-AzureStorageKey -StorageAccountName $StorageAccountName
        $storageAccountKeyPrimary = $storageAccountKey.Primary

        # Create SQL Job Definition
        [string]$JobDefinitionText = (Get-Content -LiteralPath (Join-Path $PSScriptRoot -ChildPath $JobDefinitionPath)).
                                    Replace("_StreamAnalyticsJobName",$StreamAnalyticsSQLJobName).
                                    Replace("_Location",$Location).
                                    Replace("_ConsumerGroupName",$ConsumerGroupName).
                                    Replace("_EventHubName",$EventHubName).
                                    Replace("_ServiceBusNamespace",$ServiceBusNamespaceName).
                                    Replace("_EventHubSharedAccessPolicyName",$EventHubSharedAccessPolicyName).
                                    Replace("_EventHubSharedAccessPolicyKey",$EventHubSharedAccessPolicyKey).
                                    Replace("_AccountName",$StorageAccountName).
                                    Replace("_AccountKey",$storageAccountKeyPrimary).
                                    Replace("_Container",$ContainerName).
                                    Replace("_RefdataContainer",$RefdataContainerName).
                                    Replace("_DBName",$SqlDatabaseName).
                                    Replace("_DBPassword",$SqlServerAdminPassword).
                                    Replace("_DBServer",$SqlServerName).
                                    Replace("_DBUser",$SqlServerAdminLogin)

    Provision-StreamAnalyticsJob -ServiceBusNamespace $ServiceBusNamespaceName `
                                            -EventHubName $EventHubName `
                                            -EventHubSharedAccessPolicyName $EventHubSharedAccessPolicyName `
                                            -StorageAccountName $StorageAccountName `
                                            -ContainerName $ContainerName `
                                            -ResourceGroupName $ResourceGroupName `
                                            -Location $Location `
                                            -JobDefinitionText $JobDefinitionText
    
    Write-Output "Provision Finished OK"      #>                                         
}