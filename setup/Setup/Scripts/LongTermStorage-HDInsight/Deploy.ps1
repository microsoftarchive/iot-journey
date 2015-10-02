# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

[CmdletBinding()]
Param
(
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SubscriptionName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][bool]$AddAccount = $True,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ClusterStorageAccountName = "$($ApplicationName)sa",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ContainerName = "hdinsight",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ClusterName =$ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ClusterStorageType = "Standard_LRS",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ClusterLoginUserName = "admin",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][securestring]$ClusterLoginPassword,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][int]$ClusterWorkerNodeCount = 2,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ResourceGroupName = "IoTJourney",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$DeploymentName = "LongTermStorage-HDInsight",
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

    #Sanitize input
    $ClusterStorageAccountName = $ClusterStorageAccountName.ToLower()
    $ClusterName = $ClusterName.ToLower()

    Load-Module -ModuleName Validation -ModuleLocation $ModulesFolderPath
    Load-Module -ModuleName ResourceManager -ModuleLocation $ModulesFolderPath

    # Validate input.
    Test-OnlyLettersAndNumbers "StorageAccountName" $ClusterStorageAccountName
    Test-OnlyLettersNumbersAndHyphens "ContainerName" $ContainerName

    if($AddAccount)
    {
        Add-AzureAccount
    }

    Select-AzureSubscription $SubscriptionName

    $templatePath = (Join-Path $PSScriptRoot -ChildPath ".\azuredeploy.json")

    Invoke-InAzureResourceManagerMode ({
    
        New-AzureResourceGroupIfNotExists -ResourceGroupName $ResourceGroupName -Location $Location
    
        $info = New-AzureResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
                                                 -Name $DeploymentName `
                                                 -TemplateFile $templatePath `
                                                 -clusterName $ClusterName `
                                                 -clusterLoginUserName $ClusterLoginUserName `
                                                 -clusterLoginPassword $ClusterLoginPassword `
                                                 -clusterStorageAccountName $ClusterStorageAccountName `
                                                 -clusterStorageType $ClusterStorageType `
                                                 -clusterWorkerNodeCount $ClusterWorkerNodeCount
    })
}