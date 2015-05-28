[CmdletBinding()] 
Param( 
    [string]$DBName = "fabrikamdb01",
    [string]$DBPassword = "MyPassword",
    [string]$DBServer = "fabrikamdbserver01",
    [string]$DBUser="fabrikamuser01",
	#[Parameter (Mandatory = $true)]
	[string]$SubscriptionName = "Azure Guidance",

    [String]$ResourceGroupPrefix = "Fabrikam",

    [Parameter(Mandatory=$True)][String]$StorageAccountName,
     
    [Parameter(Mandatory=$False)][String]$StreamAnalyticsJobName = "fabrikamstreamjob03",   
    
    [ValidatePattern("^[A-Za-z][-A-Za-z0-9]*[A-Za-z0-9]$")] #needs to start with letter or number, and contain only letters, numbers, and hyphens.
    [Parameter(Mandatory=$False)][String]$ServiceBusNamespace="fabrikam-ns01", 
    
    [ValidatePattern("^[A-Za-z0-9]$|^[A-Za-z0-9][\w-\.\/]*[A-Za-z0-9]$")] # needs to start with letter or number, and contain only letters, numbers, periods, hyphens, and underscores.                                   
    [Parameter(Mandatory=$False)][String]$EventHubName = "eventhub01",
                         
    [Parameter(Mandatory=$False)][String]$ServiceBusRuleName = "ManagePolicy",
                   
    [Parameter(Mandatory=$False)][String]$ConsumerGroupName= "consumergroup01",
 
    [Parameter(Mandatory=$False)][String]$EventHubSharedAccessPolicyName = "ManagePolicy",
       
    [Parameter(Mandatory=$False)][String]$StorageContainerName = "container01",
   
    [Parameter(Mandatory=$False)][String]$JobDefinitionPath = "StreamAnalyticsJobDefinition.json",# optional default to C:\StreamAnalyticsJobDefinition.json
    
    [Parameter(Mandatory=$False)][String]$Location = "Central US",
    
    [Parameter(Mandatory=$False)][String]$ResourceGroupName = $ResourceGroupPrefix + "-" + $Location.Replace(" ","-")
)
        
$VerbosePreference = "SilentlyContinue" 
Switch-AzureMode -Name AzureServiceManagement
$VerbosePreference = "Continue" 

Write-Verbose ("Resource Group Name: " + $ResourceGroupName)

#Add-AzureAccount

Select-AzureSubscription -SubscriptionName $SubscriptionName

try
{
    # WARNING: Make sure to reference the latest version of the \Microsoft.ServiceBus.dll 
    Write-Verbose "Adding the [Microsoft.ServiceBus.dll] assembly to the script..." 
    $scriptPath = Split-Path (Get-Variable MyInvocation -Scope 0).Value.MyCommand.Path
    $packagesFolder = (Split-Path $scriptPath -Parent) + "\src\packages"
    $assembly = Get-ChildItem $packagesFolder -Include "Microsoft.ServiceBus.dll" -Recurse
    Add-Type -Path $assembly.FullName

    Write-Verbose "The [Microsoft.ServiceBus.dll] assembly has been successfully added to the script." 
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
    Write-Verbose "Can not find the Shared Access Key for Manage in event hub"
}

# Get Storage Account Key
$storageAccountKey = Get-AzureStorageKey -StorageAccountName $StorageAccountName
$storageAccountKeyPrimary = $storageAccountKey.Primary

$JobDefinitionText = (get-content $JobDefinitionPath).
                    Replace("_StreamAnalyticsJobName",$StreamAnalyticsJobName).
                    Replace("_Location",$Location).
                    Replace("_ConsumerGroupName",$ConsumerGroupName).
                    Replace("_EventHubName",$EventHubName).
                    Replace("_ServiceBusNamespace",$ServiceBusNamespace).
                    Replace("_EventHubSharedAccessPolicyName",$EventHubSharedAccessPolicyName).
                    Replace("_EventHubSharedAccessPolicyKey",$EventHubSharedAccessPolicyKey).
                    Replace("_AccountName",$StorageAccountName).
                    Replace("_AccountKey",$storageAccountKeyPrimary).
                    Replace("_Container",$StorageContainerName).
                    Replace("_DBName",$DBName).
                    Replace("_DBPassword",$DBPassword).
                    Replace("_DBServer",$DBServer).
                    Replace("_DBUser",$DBUser)

$TempFileName = [guid]::NewGuid().ToString() + ".json"

$JobDefinitionText > $TempFileName

$VerbosePreference = "SilentlyContinue" 
Switch-AzureMode AzureResourceManager
$VerbosePreference = "Continue" 

# Check if the namespace already exists or needs to be created 
try
{ 
    $ResourceGroup = Get-AzureResourceGroup -Name $ResourceGroupName
    Write-Verbose "The ResourceGroup [$ResourceGroupName] already exists"  
} 
catch
{ 
    Write-Verbose "The [$ResourceGroupName] ResourceGroup does not exist." 
    Write-Verbose "Creating the [$ResourceGroupName] ResourceGroup..." 
    New-AzureResourceGroup -Name $ResourceGroupName -Location $Location
    $ResourceGroup = Get-AzureResourceGroup -Name $ResourceGroupName
    Write-Verbose "The [$ResourceGroupName] Resource Group in the [$Location] region has been successfully created." 
} 

try
{
    New-AzureStreamAnalyticsJob -ResourceGroupName $ResourceGroupName  -File $TempFileName -Force
}
catch [Exception]
{
    Write-Verbose $_.Exception.Message
    throw
}
finally
{
    if (Test-Path $TempFileName) 
    {
        Write-Verbose "deleting the temp file ... "
        Clear-Content $TempFileName
        Remove-Item $TempFileName
    }
}

$VerbosePreference = "SilentlyContinue" 
Switch-AzureMode -Name AzureServiceManagement
$VerbosePreference = "Continue" 

Write-Verbose "Create Azure StreamAnalyticsJob Completed"
