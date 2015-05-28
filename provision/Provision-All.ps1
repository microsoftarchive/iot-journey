Param
(
	[Parameter (Mandatory = $true)]
    [ValidatePattern("^[A-Za-z][-A-Za-z0-9]*[A-Za-z0-9]$")]               # needs to start with letter or number, and contain only letters, numbers, and hyphens.
    [String]$ServiceBusNamespace ="fabrikam-ns01",                                   

	[Parameter (Mandatory = $true)]
    # dont use this: [ValidatePattern("^[a-z0-9]*$")]  # don't use this, powershell script is case insensitive, uppercase letter still pass as valid 
    [ValidateScript({
      # we need to use cmathch which is case sensitive, don't use match
      If ($_ -cmatch "^[a-z0-9]*$") {                         # needs contain only lower case letters and numbers.
        $True
      }else {
        Throw "`n---Storage account name can only contain lowercase letters and numbers!---"
      }
    })]
    [String]$StorageAccountName = "fabrikamstorage01",    

	[Parameter (Mandatory = $true)]
    [String]$StreamAnalyticsJobName = "fabrikamstreamjob01",

	[Parameter (Mandatory = $true)]
	[string]$SubscriptionName = "Azure Guidance",

    [String]$Location = "Central US",               

    [String]$ResourceGroupPrefix = "Fabrikam",
    
    [ValidatePattern("^[A-Za-z0-9]$|^[A-Za-z0-9][\w-\.\/]*[A-Za-z0-9]$")] # needs to start with letter or number, and contain only letters, numbers, periods, hyphens, and underscores.
    [String]$EventHubName = "eventhub01",                   
    
    [String]$ServiceBusRuleName = "ManagePolicy",      

    [String]$ConsumerGroupName= "consumergroup01", 

    [String]$EventHubSharedAccessPolicyName = "ManagePolicy",
             
    [ValidateScript({
      # we need to use cmathch which is case sensitive, don't use match
      If ($_ -cmatch "^[a-z0-9]*$") {                         # needs contain only lower case letters and numbers.
        $True
      }else {
        Throw "`n---Storage account name can only contain lowercase letters and numbers!---"
      }
    })]
    [String]$StorageContainerName = "container01",

    #Storage containter should not be shared in HDInsight, even between clusters. 
    #see: https://azure.microsoft.com/en-us/documentation/articles/hdinsight-provision-clusters/
    [Parameter(Mandatory=$False)][string]$HDInsightStorageContainerName = "iot-hdicontainer01",

    [Parameter(Mandatory=$False)][string]$HDInsightClusterName = "iot-hdicluster01",

    [Parameter(Mandatory=$False)][string]$HDInsightClusterNodes = 2
)

# Make the script stop on error
# Set the output level to verbose and make the script stop on error 

$VerbosePreference = "SilentlyContinue" 
Switch-AzureMode -Name AzureServiceManagement
$VerbosePreference = "Continue" 

$ErrorActionPreference = "Stop" 


# Check the azure module is installed
if(-not(Get-Module -name "Azure")) 
{ 
    if(Get-Module -ListAvailable | Where-Object { $_.name -eq "Azure" }) 
    { 
        Import-Module Azure
    }
    else
    {
        "Microsoft Azure Powershell has not been installed, or cannot be found."
        Exit
    }
}


Add-AzureAccount

$VerbosePreference = "SilentlyContinue" 
.\Provision-EventHub.ps1 -SubscriptionName $SubscriptionName -Location $Location -Namespace $ServiceBusNamespace -EventHubName $EventHubName -ConsumerGroupName $ConsumerGroupName -EventHubSharedAccessPolicyName $EventHubSharedAccessPolicyName 

.\Provision-StorageAccount.ps1 -SubscriptionName $SubscriptionName -Location $Location -Name $StorageAccountName -ContainerName $StorageContainerName

.\Provision-StreamAnalyticsJob.ps1 -SubscriptionName $SubscriptionName -Location $Location -ResourceGroupPrefix $ResourceGroupPrefix -ServiceBusNamespace $ServiceBusNamespace -EventHubName $EventHubName -ServiceBusRuleName $ServiceBusRuleName -ConsumerGroupName $ConsumerGroupName -EventHubSharedAccessPolicyName $EventHubSharedAccessPolicyName -StorageAccountName $StorageAccountName -StorageContainerName $StorageContainerName -StreamAnalyticsJobName $StreamAnalyticsJobName

.\Provision-HDInsight.ps1 -SubscriptionName $SubscriptionName -StorageAccountName $StorageAccountName -StorageContainerName $HDInsightStorageContainerName -ClusterName $HDInsightClusterName -CluserNodes $HDInsightClusterNodes -Location $Location

$VerbosePreference = "Continue" 
Write-Verbose "Provision-All completed"