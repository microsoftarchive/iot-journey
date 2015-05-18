<#
.SYNOPSIS
Storage Account Provisioning - Create a new storage account

.DESCRIPTION
This script creates a new Azure Storage Account in the chosen location, configured for Local Redundancy and hourly monitoring turned on. This storage account will be used for:
 - Event Hubs checkpoint account
 - Storing poison messages
 - Storing cold data

.PARAMETER SubscriptionName
The name of the subscription to use.

.PARAMETER Name
The name of the storage account.

.PARAMETER Location
The location of the storage account
#>

$Name = "fabrikamstorage01"
$Location = "West US"
$ContainerName = "fabrikam-container01"

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
