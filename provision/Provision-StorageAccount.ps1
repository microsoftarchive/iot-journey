[CmdletBinding()]
Param
(

	[Parameter (Mandatory = $true)]
    [string]$SubscriptionName = "Azure Guidance",

	[Parameter (Mandatory = $true)]
     [string]$Location = "Central US",                   

    #[ValidatePattern("^[a-z0-9]*$")]                         # don't use this, powershell script is case insensitive, uppercase letter still pass as valid 
    [ValidateScript({
      # we need to use cmatch which is case sensitive, don't use match
      If ($_ -cmatch "^[a-z0-9]*$") {                         # needs contain only lower case letters and numbers.
        $True
      }else {
        Throw "`n---Storage account name can only contain lowercase letters and numbers!---"
      }
    })]
 	[Parameter (Mandatory = $true)]
    [string]$StorageAccountName = "fabrikamstorage01",            

 	[Parameter (Mandatory = $true)]
    [string]$ContainerName = "container01"  
)

#Add-AzureAccount

$VerbosePreference = "SilentlyContinue" 
Switch-AzureMode -Name AzureServiceManagement
$VerbosePreference = "Continue" 

Select-AzureSubscription -SubscriptionName $SubscriptionName

$storageAccount = Get-AzureStorageAccount -StorageAccountName $StorageAccountName -ErrorAction SilentlyContinue

if (!$storageAccount)
{
    # Create a new storage account
    Write-Verbose "";
    Write-Verbose ("Configuring storage account {0} in location {1}" -f $StorageAccountName, $Location);

    New-AzureStorageAccount -StorageAccountName $StorageAccountName -Location $Location -Verbose;
}
else
{
    "Storage account {0} already exists." -f $StorageAccountName
}

# Get the access key of the storage account
$key = Get-AzureStorageKey -StorageAccountName $StorageAccountName
$context = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $key.Primary;

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
Set-AzureStorageAccount -StorageAccountName $StorageAccountName -Type "Standard_LRS" -Verbose;
Write-Verbose ("Finished configuring storage account {0} in location {1}" -f $StorageAccountName, $Location);
