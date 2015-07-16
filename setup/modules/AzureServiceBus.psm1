############################
##
## Azure Service Bus
##
############################

function Provision-EventHub
{
    [CmdletBinding()]
    param 
    (
        [Parameter(Mandatory=$True)][string]$SubscriptionName,
        [Parameter(Mandatory=$True)][string]$ServiceBusNamespace,
        [Parameter(Mandatory=$True)][string]$EventHubName,
        [Parameter(Mandatory=$True)][string]$EventHubSharedAccessPolicyName,
        [Parameter(Mandatory=$True)][string]$Location,
        [Parameter(Mandatory=$False)][string]$ConsumerGroupName,
        [Parameter(Mandatory=$False)][int]$PartitionCount,
        [Parameter(Mandatory=$False)][int]$MessageRetentionInDays,
        [Parameter(Mandatory=$False)][object]$UserMetadata,
        [Parameter(Mandatory=$False)][object]$ConsumerGroupUserMetadata
    )
    PROCESS
    {
        $startTime = Get-Date
        
        Select-AzureSubscription -SubscriptionName $SubscriptionName

        Assert-ServiceBusDll

        New-ServicBusNamespaceIfNotExists -ServiceBusNamespace $ServiceBusNamespace `
                                          -Location $Location
        
        $EventHubInfo = New-EventHubIfNotExists -ServiceBusNamespace $ServiceBusNamespace `
                                                -EventHubName $EventHubName `
                                                -PartitionCount $PartitionCount `
                                                -MessageRetentionInDays $MessageRetentionInDays `
                                                -UserMetadata $UserMetadata
        
        if($ConsumerGroupName)
        {
            New-ConsumerGroupIfNotExists -ConsumerGroupName $ConsumerGroupName `
                                         -EventHubName $EventHubName `
                                         -ConsumerGroupUserMetadata $ConsumerGroupUserMetadata
        }

        $finishTime = Get-Date 
 
        # Output the time consumed in seconds 
        $TotalTime = ($finishTime - $startTime).TotalSeconds 
        Write-Verbose "The script completed in $TotalTime seconds."


        return $EventHubInfo
    }
}

function Assert-ServiceBusDll
{
    $ServiceBusDllName = "Microsoft.ServiceBus.dll"

    try
    {
        $Configuration = Get-Configuration

        Write-Output "Adding the [$ServiceBusDllName] assembly to the script..." 
        
        $packagesFolder = $Configuration.PackagesFolderPath
        $assembly = Get-ChildItem $packagesFolder -Include "$ServiceBusDllName" -Recurse
        Add-Type -Path $assembly.FullName

        Write-Output "The [$ServiceBusDllName] assembly has been successfully added to the script." 
    }
    catch
    {
        Write-Error("Could not add the $ServiceBusDllName assembly to the script. Make sure you build the solution before running the provisioning script.")
    }
}

function Get-EventHubSharedAccessPolicyKey
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$True)][string]$ServiceBusNamespace,
        [Parameter(Mandatory=$True)][string]$EventHubName,
        [Parameter(Mandatory=$True)][string]$EventHubSharedAccessPolicyName
    )
    PROCESS
    {
        $sbr = Get-AzureSBAuthorizationRule -Namespace $ServiceBusNamespace
        $NamespaceManager = [Microsoft.ServiceBus.NamespaceManager]::CreateFromConnectionString($sbr.ConnectionString); 
        $EventHub = $NamespaceManager.GetEventHub($EventHubName);
        
        $Rule = $null

        if($EventHub.Authorization.TryGetSharedAccessAuthorizationRule($EventHubSharedAccessPolicyName, [ref]$Rule))
        {
            return $Rule.PrimaryKey
        }

        throw "Can not find the Shared Access Key for Manage in event hub"
    }
}

#Private

function New-ServicBusNamespaceIfNotExists
{
    [CmdletBinding()]
    param 
    (
        [Parameter(Mandatory=$True)][string]$ServiceBusNamespace,
        [Parameter(Mandatory=$True)][string]$Location
    )
    PROCESS
    {
        $CurrentNamespace = Get-AzureSBNamespace -Name $ServiceBusNamespace

        if(!(Get-AzureSBNamespace -Name $ServiceBusNamespace))
        {
            Write-Verbose "The [$ServiceBusNamespace] namespace does not exist." 
            Write-Verbose "Creating the [$ServiceBusNamespace] namespace in the [$Location] region..." 
            
            New-AzureSBNamespace -Name $ServiceBusNamespace -Location $Location -CreateACSNamespace $False -NamespaceType Messaging

            Write-Verbose "The [$ServiceBusNamespace] namespace in the [$Location] region has been successfully created."
            
            return 
        }

        Write-Verbose "The namespace [$ServiceBusNamespace] already exists in the [$($CurrentNamespace.Region)] region."

        return
    }
}

function New-EventHubIfNotExists
{
    [CmdletBinding()]
    param 
    (
        [Parameter(Mandatory=$True)][string]$ServiceBusNamespace,
        [Parameter(Mandatory=$True)][string]$EventHubName,
        [Parameter(Mandatory=$False)][int]$PartitionCount,
        [Parameter(Mandatory=$False)][int]$MessageRetentionInDays,
        [Parameter(Mandatory=$False)][object]$UserMetadata,
        [Parameter (Mandatory = $False)][int]$RetryCountMax = 5, 
        [Parameter (Mandatory = $False)][int]$RetryDelaySeconds = 5
    )
    PROCESS
    {
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

        # TODO: Change this code to create a key with Send Permission only.
        $EventHubRuleName = "RootManageSharedAccessKey"
        $index = $sbr.ConnectionString.IndexOf("SharedAccessKey=") + 16;
        $EventHubRuleKey = $sbr.ConnectionString.Substring($index,44);

        # return
        # Bug: The following returns a ConnectionString to EventHubInfo.  Which also loads AzureServiceBus DLL that adds an identical 
        # ConnectionString property. We have a naming clash with that property and start returning 2 ConnectionString Properties
        # Simple fix is to give our ConnectionString a unique name. 
        @{
            'EventHubNamespace'= $ServiceBusNamespace;
            'EventHubName' = $EventHubName;
            'EventHubSasKeyName' = $EventHubRuleName;
            'EventHubPrimaryKey' = $EventHubRuleKey;
            'EventHubTokenLifetimeDays' = $MessageRetentionInDays;
            'ConnectionStringFix' = $sbr.ConnectionString;
        }
    }
}

function New-ConsumerGroupIfNotExists
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$True)][string]$ConsumerGroupName,
        [Parameter(Mandatory=$True)][string]$EventHubName,
        [Parameter(Mandatory=$False)][object]$ConsumerGroupUserMetadata
    )
    PROCESS
    {
        $sbr = Get-AzureSBAuthorizationRule -Namespace $ServiceBusNamespace
        # Create the NamespaceManager object to create the event hub 
        Write-Verbose "Creating a NamespaceManager object for the [$ServiceBusNamespace] namespace..." 
        $NamespaceManager = [Microsoft.ServiceBus.NamespaceManager]::CreateFromConnectionString($sbr.ConnectionString); 
        Write-Verbose "NamespaceManager object for the [$ServiceBusNamespace] namespace has been successfully created." 

        Write-Verbose "Creating the consumer group [$ConsumerGroupName] for the [$EventHubName] EventHub..." 
        $ConsumerGroupDescription = New-Object -TypeName Microsoft.ServiceBus.Messaging.ConsumerGroupDescription -ArgumentList $EventHubName, $ConsumerGroupName 
        $ConsumerGroupDescription.UserMetadata = $ConsumerGroupUserMetadata 
        $NamespaceManager.CreateConsumerGroupIfNotExists($ConsumerGroupDescription); 
        Write-Verbose "The consumer group [$ConsumerGroupName] for the [$EventHubName] EventHub has been successfully created." 
    }
}

Export-ModuleMember Provision-EventHub
Export-ModuleMember Assert-ServiceBusDll
Export-ModuleMember Get-EventHubSharedAccessPolicyKey