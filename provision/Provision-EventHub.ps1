[CmdletBinding()] 
Param ( 
	[ValidateNotNullOrEmpty()]
	[Parameter (Mandatory = $True)]
	[string]$SubscriptionName,               

	[ValidateNotNullOrEmpty()]
	[Parameter (Mandatory = $True)]
	[String]$ServiceBusNamespace,                                   

	[ValidateNotNullOrEmpty()]
	[Parameter (Mandatory = $False)]
    [ValidatePattern("^[A-Za-z0-9]$|^[A-Za-z0-9][\w-\.\/]*[A-Za-z0-9]$")] # needs to start with letter or number, and contain only letters, numbers, periods, hyphens, and underscores.
	[String]$EventHubName = "eventhub01",                   

	[ValidateNotNullOrEmpty()]
	[Parameter (Mandatory = $False)]
    [ValidatePattern("^[A-Za-z][-A-Za-z0-9]*[A-Za-z0-9]$")]      # needs to start with letter or number, and contain only letters, numbers, and hyphens.
	[String]$ConsumerGroupName= "consumergroup01", 

	[ValidateNotNullOrEmpty()]
	[Parameter (Mandatory = $False)]
	[String]$EventHubSharedAccessPolicyName = "ManagePolicy",

    [Parameter (Mandatory = $False)]
	[Int]$PartitionCount = 16,                     

    [Parameter (Mandatory = $False)]
	[Int]$MessageRetentionInDays = 7,              

    [Parameter (Mandatory = $False)]
	[String]$UserMetadata = $null,                 

    [Parameter (Mandatory = $False)]
	[String]$ConsumerGroupUserMetadata = $null,     

    [Parameter (Mandatory = $False)]
	[Bool]$CreateACSNamespace = $false, 

    [Parameter (Mandatory = $False)]
	[int]$RetryCountMax = 5, 

    [Parameter (Mandatory = $False)]
	[int]$RetryDelaySeconds = 5,

	[ValidateNotNullOrEmpty()]
    [Parameter (Mandatory = $False)]
	[String] $Location = "Central US"
)

.\Init.ps1 
 
# Set the output level to verbose and make the script stop on error 
$VerbosePreference = "SilentlyContinue" 
Switch-AzureMode -Name AzureServiceManagement
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
$CurrentNamespace = Get-AzureSBNamespace -Name $ServiceBusNamespace

# Check if the namespace already exists or needs to be created 
if ($CurrentNamespace) 
{ 
    Write-Verbose "The namespace [$ServiceBusNamespace] already exists in the [$($CurrentNamespace.Region)] region."  
} 
else 
{ 
    Write-Verbose "The [$ServiceBusNamespace] namespace does not exist." 
    Write-Verbose "Creating the [$ServiceBusNamespace] namespace in the [$Location] region..." 
    New-AzureSBNamespace -Name $ServiceBusNamespace -Location $Location -CreateACSNamespace $CreateACSNamespace -NamespaceType Messaging
    $CurrentNamespace = Get-AzureSBNamespace -Name $ServiceBusNamespace
    Write-Verbose "The [$ServiceBusNamespace] namespace in the [$Location] region has been successfully created." 
} 
 
$sbr = Get-AzureSBAuthorizationRule -Namespace $ServiceBusNamespace
# Create the NamespaceManager object to create the event hub 
Write-Verbose "Creating a NamespaceManager object for the [$ServiceBusNamespace] namespace..." 
$NamespaceManager = [Microsoft.ServiceBus.NamespaceManager]::CreateFromConnectionString($sbr.ConnectionString); 
Write-Verbose "NamespaceManager object for the [$ServiceBusNamespace] namespace has been successfully created." 


# Check if the event hub already exists 
if ($NamespaceManager.EventHubExists($EventHubName)) 
{ 
    Write-Verbose "The [$EventHubName] event hub already exists in the [$ServiceBusNamespace] namespace."  
} 
else 
{ 
    Write-Verbose "Creating the [$EventHubName] event hub in the [$ServiceBusNamespace] namespace: PartitionCount=[$PartitionCount] MessageRetentionInDays=[$MessageRetentionInDays]..." 
    $EventHubDescription = New-Object -TypeName Microsoft.ServiceBus.Messaging.EventHubDescription -ArgumentList $EventHubName 
    $EventHubDescription.PartitionCount = $PartitionCount 
    $EventHubDescription.MessageRetentionInDays = $MessageRetentionInDays 
    $EventHubDescription.UserMetadata = $UserMetadata 
    $EventHubDescription.Path = $EventHubName

	$RuleKey = [Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule]::GenerateRandomKey();
    $AccessRights = [Microsoft.ServiceBus.Messaging.AccessRights[]](@("Manage", "Listen", "Send"))
    $Rule = New-Object Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule($EventHubSharedAccessPolicyName, $RuleKey, $AccessRights)
    Write-Verbose "Rule created"

    $EventHubDescription.Authorization.Add($Rule); 

    $RetryCount = 0
    $EventHubCreated = $false
    while (-not $EventHubCreated) {
        try {
            $NamespaceManager.CreateEventHubIfNotExists($EventHubDescription);
            Write-Verbose "The [$EventHubName] event hub in the [$ServiceBusNamespace] namespace has been successfully created." 
            $EventHubCreated = $True
        } catch {
            if ($RetryCount -ge $RetryCountMax) {
                Write-Verbose ("CreateEventHubIfNotExists failed the maximum number of {0} times." -f $RetryCountMax)
                throw
            } else {
                Write-Verbose ("CreateEventHubIfNotExists failed. Retrying in {0} seconds." -f $RetryDelaySeconds)
                Start-Sleep $RetryDelaySeconds
                $RetryCount++
            }

        }

    }
} 
 
# Create the consumer group if not exists 
Write-Verbose "Creating the consumer group [$ConsumerGroupName] for the [$EventHubName] event hub..." 
$ConsumerGroupDescription = New-Object -TypeName Microsoft.ServiceBus.Messaging.ConsumerGroupDescription -ArgumentList $EventHubName, $ConsumerGroupName 
$ConsumerGroupDescription.UserMetadata = $ConsumerGroupUserMetadata 
$NamespaceManager.CreateConsumerGroupIfNotExists($ConsumerGroupDescription); 
Write-Verbose "The consumer group [$ConsumerGroupName] for the [$EventHubName] event hub has been successfully created." 

# Mark the finish time of the script execution 
$finishTime = Get-Date 
 
# Output the time consumed in seconds 
$TotalTime = ($finishTime - $startTime).TotalSeconds 
Write-Verbose "The script completed in $TotalTime seconds."
