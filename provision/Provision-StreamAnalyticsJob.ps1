[CmdletBinding(PositionalBinding=$True)] 
Param( 

	[Parameter (Mandatory = $true)]
	[string]$SubscriptionName = "Azure Guidance",

	[Parameter (Mandatory = $true)]
    [String]$Location = "Central US",                 

	[Parameter (Mandatory = $true)]
    [String]$ResourceGroupPrefix = "Fabrikam",

    [String]$ResourceGroupName = $ResourceGroupPrefix + "-" + $Location.Replace(" ","-"),

	[Parameter (Mandatory = $true)]
    [String]$StreamAnalyticsJobName = "fabrikamstreamjob01",   

    [ValidatePattern("^[A-Za-z][-A-Za-z0-9]*[A-Za-z0-9]$")]               # needs to start with letter or number, and contain only letters, numbers, and hyphens.
	[Parameter (Mandatory = $true)]
    [String]$ServiceBusNamespace="fabrikam-ns01",                                   
    
    [ValidatePattern("^[A-Za-z0-9]$|^[A-Za-z0-9][\w-\.\/]*[A-Za-z0-9]$")] # needs to start with letter or number, and contain only letters, numbers, periods, hyphens, and underscores.
	[Parameter (Mandatory = $true)]
    [String]$EventHubName = "eventhub01",                   
    
	[Parameter (Mandatory = $true)]
    [string]$DBName = "fabrikamdb01",

	[Parameter (Mandatory = $true)]
    [string]$DBPassword = "MyPassword",

	[Parameter (Mandatory = $true)]
    [string]$DBServer = "fabrikamdbserver01", #    you can also use "fabrikamdbserver01.database.windows.net"

	[Parameter (Mandatory = $true)]
    [string]$DBUser="fabrikamuser01",

	[Parameter (Mandatory = $true)]
    [String]$ConsumerGroupName= "consumergroup01", 

	[Parameter (Mandatory = $true)]
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
       
 	[Parameter (Mandatory = $true)]
    [string]$ContainerName = "container01",

    [ValidatePattern("^[A-Za-z0-9]$|^[A-Za-z0-9][\w-\.\/]*[A-Za-z0-9]$")] 
    [String]$JobDefinitionPath = "StreamAnalyticsJobDefinition.json"       # optional default to C:\StreamAnalyticsJobDefinition.json
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
if($EventHub.Authorization.TryGetSharedAccessAuthorizationRule($EventHubSharedAccessPolicyName, [ref]$Rule))
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
$RefdataContainerName = $ContainerName+"refdata"

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
                    Replace("_Container",$ContainerName).
                    Replace("_RefdataContainer",$RefdataContainerName).
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
