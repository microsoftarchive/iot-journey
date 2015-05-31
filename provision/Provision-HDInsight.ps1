[CmdletBinding()]
Param (
    [Parameter(Mandatory=$True)][string]$SubscriptionName,
    
    [ValidateScript({
      # we need to use cmathch which is case sensitive, don't use match
      If ($_ -cmatch "^[a-z0-9]*$") {                         # needs contain only lower case letters and numbers.
        $True
      }else {
        Throw "`n---Storage account name can only contain lowercase letters and numbers!---"
      }
    })]
    [Parameter(Mandatory=$True)][String]$StorageAccountName,

    [ValidatePattern("^[A-Za-z][-A-Za-z0-9]*[A-Za-z0-9]$")] #needs to start with letter or number, and contain only letters, numbers, and hyphens.
    [Parameter(Mandatory=$False)][string]$StorageContainerName = "iot-hdicontainer01",

    [Parameter(Mandatory=$False)][string]$ClusterName = "iot-hdicluster01",
    [Parameter(Mandatory=$False)][int]$ClusterNodes = 2,
    [Parameter(Mandatory=$False)][string]$Location = "Central US"
)

############################
##
## Script start up
##
############################

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
{  s
    New-AzureStorageContainer -Name $StorageContainerName -Context $DestContext
}

$HDInsightCluster = Get-AzureHDInsightCluster -Name $ClusterName

if(!$HDInsightCluster)
{
    Write-Verbose "Creating a new HDInsight cluser named: [$ClusterName]. This operation may take several minutes to complete."

    $Credential = Get-Credential -UserName "admin" -Message "Provide a password for the new cluster. The filed must contain: a lowercase letter, a number and a special character."

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


                          