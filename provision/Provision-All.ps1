
Param
(
	#[Parameter (Mandatory = $true)]
	[string]$SubscriptionName = "Azure Guidance",

    [String]$Location = "Central US",               

    [String]$ResourceGroupName = "StreamAnalytics-Default-Central-US",    

    [ValidatePattern("^[A-Za-z][-A-Za-z0-9]*[A-Za-z0-9]$")]               # needs to start with letter or number, and contain only letters, numbers, and hyphens.
    [String]$ServiceBusNamespace="fabrikam-ns01",                                   
    
    [ValidatePattern("^[A-Za-z0-9]$|^[A-Za-z0-9][\w-\.\/]*[A-Za-z0-9]$")] # needs to start with letter or number, and contain only letters, numbers, periods, hyphens, and underscores.
    [String]$EventHubName = "eventhub01",                   
    
    [String]$ServiceBusRuleName = "ManagePolicy",      

    [String]$ConsumerGroupName= "consumergroup01", 

    [String]$EventHubSharedAccessPolicyName = "ManagePolicy",
             
    [ValidatePattern("^[a-z][a-z0-9]*[a-z0-9]$")]               # needs contain only lower case letters and numbers.
    [String]$StorageAccountName = "fabrikamstorage01",    
       
    [String]$StorageContainerName = "container01",

    [String]$StreamAnalyticsJobName = "fabrikamstreamjob01"
)

# Make the script stop on error
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

#Add-AzureAccount

Select-AzureSubscription -SubscriptionName $SubscriptionName

.\Provision-EventHub.ps1 -SubscriptionName $SubscriptionName -Location $Location -Namespace $ServiceBusNamespace -EventHubName $EventHubName -ConsumerGroupName $ConsumerGroupName -EventHubSharedAccessPolicyName $EventHubSharedAccessPolicyName 


.\Provision-StorageAccount.ps1 -SubscriptionName $SubscriptionName -Location $Location -Name $StorageAccountName -ContainerName $StorageContainerName

.\Provision-StreamAnalyticsJob.ps1 -SubscriptionName $SubscriptionName -Location $Location -ResourceGroupName $ResourceGroupName -ServiceBusNamespace $ServiceBusNamespace -EventHubName $EventHubName -ServiceBusRuleName $ServiceBusRuleName -ConsumerGroupName $ConsumerGroupName -EventHubSharedAccessPolicyName $EventHubSharedAccessPolicyName -StorageAccountName $StorageAccountName -StorageContainerName $StorageContainerName -StreamAnalyticsJobName $StreamAnalyticsJobName 

