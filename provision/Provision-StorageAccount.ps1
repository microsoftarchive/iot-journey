[CmdletBinding()]
Param
(
    [Parameter(Mandatory=$True)][string]$SubscriptionName,
    [Parameter(Mandatory=$True)][string]$Name,
    
    [ValidatePattern("^[A-Za-z][-A-Za-z0-9]*[A-Za-z0-9]$")] #needs to start with letter or number, and contain only letters, numbers, and hyphens.
    [Parameter(Mandatory=$False)][string]$ContainerName = "container01",
      
    [Parameter(Mandatory=$False)][string]$Location = "Central US"  
)

#Add-AzureAccount

$VerbosePreference = "SilentlyContinue" 
Switch-AzureMode -Name AzureServiceManagement
$VerbosePreference = "Continue" 

Select-AzureSubscription -SubscriptionName $SubscriptionName

$storageAccount = Get-AzureStorageAccount -StorageAccountName $Name -ErrorAction SilentlyContinue

if (!$storageAccount)
{
    # Create a new storage account
    Write-Verbose "";
    Write-Verbose ("Configuring storage account {0} in location {1}" -f $Name, $Location);

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
Write-Verbose ("Finished configuring storage account {0} in location {1}" -f $Name, $Location);
