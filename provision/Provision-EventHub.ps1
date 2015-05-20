
[CmdletBinding(PositionalBinding=$True)] 
Param( 
    [string]$SubscriptionName = "Azure Guidance",

    [String]$Location = "Central US",                 

    [ValidatePattern("^[A-Za-z][-A-Za-z0-9]*[A-Za-z0-9]$")]      # needs to start with letter or number, and contain only letters, numbers, and hyphens.
    [String]$Namespace="fabrikam-ns01",                                   

    [ValidatePattern("^[A-Za-z0-9]$|^[A-Za-z0-9][\w-\.\/]*[A-Za-z0-9]$")] # needs to start with letter or number, and contain only letters, numbers, periods, hyphens, and underscores.

    [String]$EventHubName = "eventhub01",                   

    [String]$ConsumerGroupName= "consumergroup01", 

    [String]$EventHubSharedAccessPolicyName = "ManagePolicy",

    [Int]$PartitionCount = 16,                     

    [Int]$MessageRetentionInDays = 7,              

    [String]$UserMetadata = $null,                 

    [String]$ConsumerGroupUserMetadata = $null,     

    [Bool]$CreateACSNamespace = $false             

    ) 
 

# Set the output level to verbose and make the script stop on error 
$VerbosePreference = "Continue" 
$ErrorActionPreference = "Stop" 
 
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

 
# Mark the start time of the script execution 
$startTime = Get-Date 

# Create Azure Service Bus namespace 
$CurrentNamespace = Get-AzureSBNamespace -Name $Namespace

# Check if the namespace already exists or needs to be created 
if ($CurrentNamespace) 
{ 
    Write-Output "The namespace [$Namespace] already exists in the [$($CurrentNamespace.Region)] region."  
} 
else 
{ 
    Write-Output "The [$Namespace] namespace does not exist." 
    Write-Output "Creating the [$Namespace] namespace in the [$Location] region..." 
    New-AzureSBNamespace -Name $Namespace -Location $Location -CreateACSNamespace $CreateACSNamespace -NamespaceType Messaging
    $CurrentNamespace = Get-AzureSBNamespace -Name $Namespace
    Write-Output "The [$Namespace] namespace in the [$Location] region has been successfully created." 
} 
 
$sbr = Get-AzureSBAuthorizationRule -Namespace $Namespace
# Create the NamespaceManager object to create the event hub 
Write-Output "Creating a NamespaceManager object for the [$Namespace] namespace..." 
$NamespaceManager = [Microsoft.ServiceBus.NamespaceManager]::CreateFromConnectionString($sbr.ConnectionString); 
Write-Output "NamespaceManager object for the [$Namespace] namespace has been successfully created." 

# Check if the event hub already exists 
if ($NamespaceManager.EventHubExists($EventHubName)) 
{ 
    Write-Output "The [$EventHubName] event hub already exists in the [$Namespace] namespace."  
} 
else 
{ 
    Write-Output "Creating the [$EventHubName] event hub in the [$Namespace] namespace: PartitionCount=[$PartitionCount] MessageRetentionInDays=[$MessageRetentionInDays]..." 
    $EventHubDescription = New-Object -TypeName Microsoft.ServiceBus.Messaging.EventHubDescription -ArgumentList $EventHubName 
    $EventHubDescription.PartitionCount = $PartitionCount 
    $EventHubDescription.MessageRetentionInDays = $MessageRetentionInDays 
    $EventHubDescription.UserMetadata = $UserMetadata 
    $EventHubDescription.Path = $EventHubName
    $RuleKey = [Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule]::GenerateRandomKey();
    [Microsoft.ServiceBus.Messaging.AccessRights[]]$AccessRights = @("Manage", "Listen", "Send")
    $Rule = New-Object -TypeName Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule  -ArgumentList $EventHubSharedAccessPolicyName, $RuleKey,$AccessRights
    $EventHubDescription.Authorization.Add($Rule); 
    $NamespaceManager.CreateEventHubIfNotExists($EventHubDescription);
    Write-Output "The [$EventHubName] event hub in the [$Namespace] namespace has been successfully created." 
} 
 
# Create the consumer group if not exists 
Write-Output "Creating the consumer group [$ConsumerGroupName] for the [$EventHubName] event hub..." 
$ConsumerGroupDescription = New-Object -TypeName Microsoft.ServiceBus.Messaging.ConsumerGroupDescription -ArgumentList $EventHubName, $ConsumerGroupName 
$ConsumerGroupDescription.UserMetadata = $ConsumerGroupUserMetadata 
$NamespaceManager.CreateConsumerGroupIfNotExists($ConsumerGroupDescription); 
Write-Output "The consumer group [$ConsumerGroupName] for the [$EventHubName] event hub has been successfully created." 

# Mark the finish time of the script execution 
$finishTime = Get-Date 
 
# Output the time consumed in seconds 
$TotalTime = ($finishTime - $startTime).TotalSeconds 
Write-Output "The script completed in $TotalTime seconds."
