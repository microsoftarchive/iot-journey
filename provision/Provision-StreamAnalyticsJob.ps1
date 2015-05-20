
[CmdletBinding(PositionalBinding=$True)] 
Param( 
	#[Parameter (Mandatory = $true)]
	[string]$SubscriptionName = "Azure Guidance",

    [String]$Location = "Central US",                 

    [String]$ResourceGroupName = "StreamAnalytics-Default-Central-US",   

    [String]$StreamAnalyticsJobName = "fabrikamstreamjob01",   

    [ValidatePattern("^[A-Za-z][-A-Za-z0-9]*[A-Za-z0-9]$")]               # needs to start with letter or number, and contain only letters, numbers, and hyphens.
    [String]$ServiceBusNamespace="fabrikam-ns01",                                   
    
    [ValidatePattern("^[A-Za-z0-9]$|^[A-Za-z0-9][\w-\.\/]*[A-Za-z0-9]$")] # needs to start with letter or number, and contain only letters, numbers, periods, hyphens, and underscores.
    [String]$EventHubName = "eventhub01",                   
    
    [String]$ServiceBusRuleName = "ManagePolicy",                   

    [String]$ConsumerGroupName= "consumergroup01", 

    [String]$EventHubSharedAccessPolicyName = "ManagePolicy",

    #[ValidatePattern("^[a-z0-9]*$")]                         # don't use this, powershell script is case insensitive, uppercase letter still pass as valid 
    [ValidateScript({
      # we need to use cmathch which is case sensitive, don't use match
      If ($_ -cmatch "^[a-z0-9]*$") {                         # needs contain only lower case letters and numbers.
        $True
      }else {
        Throw "`n---Storage account name can only contain lowercase letters and numbers!---"
      }
    })]

    [String]$StorageAccountName = "fabrikamstorage01",    
       
    [String]$StorageContainerName = "container01",   

    [ValidatePattern("^[A-Za-z0-9]$|^[A-Za-z0-9][\w-\.\/]*[A-Za-z0-9]$")] 
    [String]$JobDefinitionPath = "StreamAnalyticsJobDefinition.json"       # optional default to C:\StreamAnalyticsJobDefinition.json
    ) 
        

#Add-AzureAccount

Select-AzureSubscription -SubscriptionName $SubscriptionName

try
{
    # WARNING: Make sure to reference the latest version of the \Microsoft.ServiceBus.dll 
    Write-Output "Adding the [Microsoft.ServiceBus.dll] assembly to the script..." 
    $scriptPath = Split-Path (Get-Variable MyInvocation -Scope 0).Value.MyCommand.Path
    $packagesFolder = (Split-Path $scriptPath -Parent) + "\src\packages"
    $assembly = Get-ChildItem $packagesFolder -Include "Microsoft.ServiceBus.dll" -Recurse
    Add-Type -Path $assembly.FullName

    Write-Output "The [Microsoft.ServiceBus.dll] assembly has been successfully added to the script." 
}
catch [System.Exception]
{
    Write-Error("Could not add the Microsoft.ServiceBus.dll assembly to the script. Make sure you build the solution before running the provisioning script.")
}

$sbr = Get-AzureSBAuthorizationRule -Namespace $ServiceBusNamespace
$NamespaceManager = [Microsoft.ServiceBus.NamespaceManager]::CreateFromConnectionString($sbr.ConnectionString); 
$EventHub = $NamespaceManager.GetEventHub($EventHubName);
$Rule = $null
if($EventHub.Authorization.TryGetSharedAccessAuthorizationRule($ServiceBusRuleName, [ref]$Rule))
{
    $EventHubSharedAccessPolicyKey = $Rule.PrimaryKey
}
else
{
    Write-Output "Can not find the Shared Access Key for Manage in event hub"
}

# Get Storage Account Key
$storageAccountKey = Get-AzureStorageKey -StorageAccountName $StorageAccountName


$JobDefinitionText = (get-content $JobDefinitionPath).
                    Replace("_StreamAnalyticsJobName",$StreamAnalyticsJobName).
                    Replace("_Location",$Location).
                    Replace("_ConsumerGroupName",$ConsumerGroupName).
                    Replace("_EventHubName",$EventHubName).
                    Replace("_ServiceBusNamespace",$ServiceBusNamespace).
                    Replace("_EventHubSharedAccessPolicyName",$EventHubSharedAccessPolicyName).
                    Replace("_EventHubSharedAccessPolicyKey",$EventHubSharedAccessPolicyKey).
                    Replace("_AccountName",$StorageAccountName).
                    Replace("_AccountKey",$StorageAccountKey).
                    Replace("_Container",$StorageContainerName)


$TempFileName = [guid]::NewGuid().ToString() + ".json"

$JobDefinitionText > $TempFileName


#$AzureSubscription = Get-AzureSubscription -SubscriptionName $SubscriptionName
#Select-AzureSubscription –SubscriptionId $AzureSubscription.SubscriptionId
Switch-AzureMode AzureResourceManager
New-AzureStreamAnalyticsJob -ResourceGroupName $ResourceGroupName  -File $TempFileName -Force
if (Test-Path $TempFileName) {
    Clear-Content $TempFileName
    Remove-Item $TempFileName
}

Write-Output "Create Azure StreamAnalyticsJob Completed"
