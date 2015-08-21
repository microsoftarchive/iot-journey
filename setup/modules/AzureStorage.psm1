############################
##
## Azure Storage
##
############################

<#
.SYNOPSIS
  Creates an Azure Storage Account and a container if they not exists.

.DESCRIPTION
  Creates an Azure Storage Account and a container if they not exists.

.PARAMETER StorageAccountName
    The name of the storage account.

.PARAMETER ContainerName
    The name of the container to be created within the storage account.

.PARAMETER Location
    Location

.PARAMETER Type
    [Standard_LRS (Default), Standard_ZRS, Standard_GRS, Standard_RAGRS] 
     
.EXAMPLE
  <Example goes here. Repeat this attribute for more than one example>
#>
function New-ProvisionedStorageAccount
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$True)][string]$StorageAccountName,
        [Parameter(Mandatory=$True)][string]$Location,
        [Parameter(Mandatory=$False)][string]$ContainerName,
        [Parameter(Mandatory=$False)][string]$Type = "Standard_LRS"
    )
    PROCESS
    {
        New-StorageAccountIfNotExists -StorageAccountName $StorageAccountName `
                                      -Location $Location

        # Get the access key of the storage account
        $key = Get-AzureStorageKey -StorageAccountName $StorageAccountName
        $context = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $key.Primary;

        if($ContainerName)
        {
            New-StorageContainerIfNotExists -ContainerName $ContainerName `
                                        -Context $context
        }

        # Configure options for storage account
        Set-AzureStorageAccount -StorageAccountName $StorageAccountName -Type $Type -Verbose;
        Write-Verbose ("Finished configuring storage account {0} in location {1}" -f $StorageAccountName, $Location);

        @{"AccountName" = $StorageAccountName; "AccountKey" = $key.Primary }
    }
}

function New-StorageContainerIfNotExists
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$True)][string]$ContainerName,
        [Parameter(Mandatory=$True)][object]$Context
    )
    PROCESS
    {
        if (!(Get-AzureStorageContainer -Name $ContainerName -ErrorAction SilentlyContinue -Context $context))
        {
            New-AzureStorageContainer -Context $context -Name $ContainerName

            return
        }

        Write-Verbose ("The storage container {0} already exists." -f $ContainerName)

        return
    }
}

function Set-BlobData
{
    [CmdletBinding()]
    param
    (
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$StorageAccountName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ContainerName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$BlobName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$FilePath
    )
    PROCESS
    {
        $key = Get-AzureStorageKey -StorageAccountName $StorageAccountName
        $context = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $key.Primary;
        Set-AzureStorageBlobContent -Blob $BlobName -Container $ContainerName -File $FilePath -Context $context -Force
    }
}

#private

function New-StorageAccountIfNotExists
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$True)][string]$StorageAccountName,
        [Parameter(Mandatory=$True)][string]$Location
    )
    PROCESS
    {
        if (!(Get-AzureStorageAccount -StorageAccountName $StorageAccountName -ErrorAction SilentlyContinue))
        {
            # Create a new storage account
            Write-Verbose ("Configuring storage account {0} in location {1}" -f $StorageAccountName, $Location);

            New-AzureStorageAccount -StorageAccountName $StorageAccountName -Location $Location -Verbose;

            return
        }

        Write-Verbose ("Storage account {0} already exists." -f $StorageAccountName)

        return
    }
}

Export-ModuleMember New-ProvisionedStorageAccount
Export-ModuleMember New-StorageContainerIfNotExists
Export-ModuleMember Set-BlobData