[CmdletBinding()]
Param (
    [Parameter(Mandatory=$True)][string]$SubscriptionName,
    [Parameter(Mandatory=$True)][String]$StorageAccountName,
    [Parameter(Mandatory=$True)][string]$StorageContainerName,
    [Parameter(Mandatory=$True)][string]$ClusterName,
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

# Create a Blob storage container if it does not exists.
if (!(Test-AzureName -Storage $StorageAccountName))
{  
    New-AzureStorageContainer -Name $StorageContainerName -Context $DestContext
}

$Credential = Get-Credential -UserName "admin" -Message "Provide a password for the new cluster. The filed must contain: a lowercase letter, a number and a special character."

# Create a new HDInsight cluster
New-AzureHDInsightCluster -Name $ClusterName -Location $Location `
                          -DefaultStorageAccountName "$StorageAccountName.blob.core.windows.net" `
                          -DefaultStorageAccountKey $StorageAccountKey `
                          -DefaultStorageContainerName $ClusterName `
                          -ClusterSizeInNodes $ClusterNodes `
                          -Credential $Credential
                          