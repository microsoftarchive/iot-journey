<# 
    .SYNOPSIS 
    This script can be used to provision a namespace and an event hub. 
             
    .DESCRIPTION 
    This script can be used to provision a namespace and an event hub.  
    In particular, the script allows to specify the following parameters: 
    -- Event Hub path 
    -- Event hub message retention in days 
    -- Event Hub partition count 
    -- Event Hub user metadata 
    -- Service Bus Namespace 
    -- Azure Datacenter location 
     
    .PARAMETER  Path 
    Specifies the full path of the event hub. 
 
    .PARAMETER  PartitionCount 
    Specifies the current number of shards on the event hub. 
 
    .PARAMETER  MessageRetentionInDays 
    Specifies the number of days to retain the events for this event hub. 
 
    .PARAMETER  UserMetadata 
    Specifies the user metadata for the event hub. 
     
    .PARAMETER  ConsumerGroupName 
    Specifies the name of a custom consumer group. 
 
    .PARAMETER  ConsumerGroupUserMetadata 
    Specifies the user metadata for the custom consumer group. 
 
    .PARAMETER  Namespace 
    Specifies the name of the Service Bus namespace. 
 
    .PARAMETER  CreateACSNamespace 
    Specifies whether  to create an ACS namespace associated to the Service Bus namespace. 
 
    .PARAMETER  Location 
    Specifies the location to create the namespace in. The default value is "West Europe".  
 
    Valid values: 
    -- East Asia 
    -- East US 
    -- North Central US 
    -- North Europe 
    -- West Europe 
    -- West US 
     
    .NOTES   
    Author     : Paolo Salvatori 
    Twitter    : @babosbird 
    Blog       : http://blogs.msdn.com/b/paolos/ 
#> 
 
[CmdletBinding(PositionalBinding=$True)] 
Param( 
    [ValidatePattern("^[A-Za-z0-9]$|^[A-Za-z0-9][\w-\.\/]*[A-Za-z0-9]$")] # needs to start with letter or number, and contain only letters, numbers, periods, hyphens, and underscores.
    [String]$Path = "fabrikam-eventhub01",          # optional    default to "fabrikam-eventhub01"
    [Int]$PartitionCount = 16,                      # optional    default to 16 
    [Int]$MessageRetentionInDays = 7,               # optional    default to 7 
    [String]$UserMetadata = $null,                  # optional    default to $null 
    [String]$StreamAnalyticsConsumerGroupName= "fabrikam-consumergroup01", # optional    default to "fabrikam-consumergroup01" 
    [String]$ConsumerGroupUserMetadata = $null,     # optional    default to $null 
    [ValidatePattern("^[A-Za-z][-A-Za-z0-9]*[A-Za-z0-9]$")] 
    [String]$Namespace="fabrikam-eventhub01-ns",    # optional    needs to start with letter or number, and contain only letters, numbers, and hyphens.
    [Bool]$CreateACSNamespace = $false,             # optional    default to $false 
    [String]$Location = "West US",                  # optional    default to "West US" 
    [String]$RuleName = "Manage"                    # optional    default to "Manage" 
    ) 
 

# Set the output level to verbose and make the script stop on error 
$VerbosePreference = "Continue" 
$ErrorActionPreference = "Stop" 
 
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
if ($NamespaceManager.EventHubExists($Path)) 
{ 
    Write-Output "The [$Path] event hub already exists in the [$Namespace] namespace."  
} 
else 
{ 
    Write-Output "Creating the [$Path] event hub in the [$Namespace] namespace: PartitionCount=[$PartitionCount] MessageRetentionInDays=[$MessageRetentionInDays]..." 
    $EventHubDescription = New-Object -TypeName Microsoft.ServiceBus.Messaging.EventHubDescription -ArgumentList $Path 
    $EventHubDescription.PartitionCount = $PartitionCount 
    $EventHubDescription.MessageRetentionInDays = $MessageRetentionInDays 
    $EventHubDescription.UserMetadata = $UserMetadata 
    $EventHubDescription.Path = $Path
    $RuleKey = [Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule]::GenerateRandomKey();
    [Microsoft.ServiceBus.Messaging.AccessRights[]]$AccessRights = @("Manage", "Listen", "Send")
    $Rule = New-Object -TypeName Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule  -ArgumentList $RuleName, $RuleKey,$AccessRights
    $EventHubDescription.Authorization.Add($Rule); 
    $NamespaceManager.CreateEventHubIfNotExists($EventHubDescription);
    Write-Output "The [$Path] event hub in the [$Namespace] namespace has been successfully created." 
} 
 
# Create the consumer group if not exists 
Write-Output "Creating the consumer group [$StreamAnalyticsConsumerGroupName] for the [$Path] event hub..." 
$ConsumerGroupDescription = New-Object -TypeName Microsoft.ServiceBus.Messaging.ConsumerGroupDescription -ArgumentList $Path, $StreamAnalyticsConsumerGroupName 
$ConsumerGroupDescription.UserMetadata = $ConsumerGroupUserMetadata 
$NamespaceManager.CreateConsumerGroupIfNotExists($ConsumerGroupDescription); 
Write-Output "The consumer group [$StreamAnalyticsConsumerGroupName] for the [$Path] event hub has been successfully created." 

# Mark the finish time of the script execution 
$finishTime = Get-Date 
 
# Output the time consumed in seconds 
$TotalTime = ($finishTime - $startTime).TotalSeconds 
Write-Output "The script completed in $TotalTime seconds."
