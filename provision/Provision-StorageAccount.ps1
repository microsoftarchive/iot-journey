[CmdletBinding()]
Param
(
	[ValidateNotNullOrEmpty()]
	[Parameter (Mandatory = $True)]
	[string]$SubscriptionName,                  

 	[ValidateNotNullOrEmpty()]
	[Parameter (Mandatory = $True)]
    [ValidateScript({
      # we need to use cmatch which is case sensitive, don't use match
      If ($_ -cmatch "^[a-z0-9]*$") {                         # need contain only lower case letters and numbers.
        $True
      }else {
        Throw "`n---Storage account name can only contain lowercase letters and numbers!---"
      }
    })]
	[String]$StorageAccountName,               

 	[ValidateNotNullOrEmpty()]
 	[Parameter (Mandatory = $False)]
    [ValidatePattern("^[A-Za-z][-A-Za-z0-9]*[A-Za-z0-9]$")] #needs to start with letter or number, and contain only letters, numbers, and hyphens.
	[string]$ContainerName = "container01",
	
	[ValidateNotNullOrEmpty()]
    [Parameter (Mandatory = $False)]
	[String]$Location = "Central US"  
)

.\Init.ps1

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

$RefContainerName = $ContainerName + "refdata"
$RefContainer = Get-AzureStorageContainer -Name $RefContainerName -ErrorAction SilentlyContinue -Context $context
if (!$RefContainer)
{
    New-AzureStorageContainer -Context $context -Name $RefContainerName
}
else 
{
    "The storage container {0} already exists." -f $RefContainerName
}


# Configure options for storage account
Set-AzureStorageAccount -StorageAccountName $StorageAccountName -Type "Standard_LRS" -Verbose;
Write-Verbose ("Finished configuring storage account {0} in location {1}" -f $StorageAccountName, $Location);

@{"AccountName" = $StorageAccountName; "AccountKey" = $key.Primary }
