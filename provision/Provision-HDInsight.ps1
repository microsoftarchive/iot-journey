[CmdletBinding()]
Param (
    
	[ValidateNotNullOrEmpty()]
	[Parameter (Mandatory = $True)]
	[string]$SubscriptionName,   
    
	[ValidateNotNullOrEmpty()]
    [Parameter(Mandatory=$True)]
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
    [Parameter(Mandatory=$False)]
    [ValidatePattern("^[A-Za-z][-A-Za-z0-9]*[A-Za-z0-9]$")] #needs to start with letter or number, and contain only letters, numbers, and hyphens.
	[string]$StorageContainerName = "iot-hdicontainer01",

	[ValidateNotNullOrEmpty()]
    [Parameter(Mandatory=$False)]
	[string]$ClusterName = "iot-hdicluster01",

    [Parameter(Mandatory=$False)]
	[int]$ClusterNodes = 2,

    [ValidateNotNullOrEmpty()]
    [Parameter (Mandatory = $false)]
	[String] $Location = "Central US"
)

.\Init.ps1

# Set the output level to verbose and make the script stop on error.
$VerbosePreference = "SilentlyContinue" 
Switch-AzureMode -Name AzureServiceManagement
$VerbosePreference = "Continue" 
$ErrorActionPreference = "Stop"

$StorageAccountKey = Get-AzureStorageKey $StorageAccountName | %{ $_.Primary }

# Create a storage context object
$DestContext = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $StorageAccountKey  

$CurrentStorageContainer = Get-AzureStorageContainer -Name $StorageContainerName -Context $DestContext -ErrorAction SilentlyContinue
# Create a Blob storage container if it does not exists.
if (!$CurrentStorageContainer)
{
    New-AzureStorageContainer -Name $StorageContainerName -Context $DestContext
}

$HDInsightCluster = Get-AzureHDInsightCluster -Name $ClusterName

if(!$HDInsightCluster)
{
    Write-Verbose "Creating a new HDInsight cluster named: [$ClusterName]. This operation may take several minutes to complete."

    $Credential = Get-Credential -UserName "admin" -Message "Provide a password for the new HDInsight cluster. The password must contain lowercase letters, numbers, and special characters."

    # Create a new HDInsight cluster
    New-AzureHDInsightCluster -Name $ClusterName -Location $Location `
                              -DefaultStorageAccountName "$StorageAccountName.blob.core.windows.net" `
                              -DefaultStorageAccountKey $StorageAccountKey `
                              -DefaultStorageContainerName $ClusterName `
                              -ClusterSizeInNodes $ClusterNodes `
                              -Credential $Credential
}
else
{
    Write-Verbose "An HDInsight cluster named: [$ClusterName] already exists."
}


                          