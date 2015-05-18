<#
.SYNOPSIS
Storage Account Provisioning - Create a new storage account

.DESCRIPTION
This script creates a new Azure Storage Account in the chosen location, This storage account will be used for:
 - Stream Analytics Job Output 
 
.PARAMETER Name
The name of the storage account.

.PARAMETER Location
The location of the storage account

.PARAMETER ContainerName
The Container Name of the storage account
#>

Param
(
    $Name = "fabrikamstorage01",             # optional default to fabrikamstorage01
    $Location = "West US",                   # optional default to West US
    $ContainerName = "fabrikam-container01"  # optional default to fabrikam-container01
)

$storageAccount = Get-AzureStorageAccount -StorageAccountName $Name -ErrorAction SilentlyContinue

if (!$storageAccount)
{
    # Create a new storage account
    Write-Output "";
    Write-Output ("Configuring storage account {0} in location {1}" -f $Name, $Location);

    New-AzureStorageAccount -StorageAccountName $Name -Location $Location -Verbose;
}
else
{
    "Storage account {0} already exists." -f $Name
}

# Get the access key of the storage account
$key = Get-AzureStorageKey -StorageAccountName $Name
$context = New-AzureStorageContext -StorageAccountName $Name -StorageAccountKey $key.Primary;

$Container = Get-AzureStorageContainer -Name $ContainerName -ErrorAction SilentlyContinue -Context $context
if (!$Container)
{
    New-AzureStorageContainer -Context $context -Name $ContainerName
}
else 
{
    "The storage container {0} already exists." -f $ContainerName
}

# Configure options for storage account
Set-AzureStorageAccount -StorageAccountName $Name -Type "Standard_LRS" -Verbose;
Write-Output ("Finished configuring storage account {0} in location {1}" -f $Name, $Location);
