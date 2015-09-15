function New-SharedAccessAuthorizationRule
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=$true)][string]$Name,
        [Parameter(Mandatory=$true)][Microsoft.ServiceBus.Messaging.AccessRights[]]$Rights
    )
    PROCESS
    {
        $policyKey = [Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule]::GenerateRandomKey()
        $rightsColl = New-Object -TypeName System.Collections.Generic.List[Microsoft.ServiceBus.Messaging.AccessRights] (,[Microsoft.ServiceBus.Messaging.AccessRights[]]$Rights)
        $accessRule = New-Object -TypeName  Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule -ArgumentList $Name, $policyKey, $rightsColl

        @{Rule = $accessRule; PolicyName = $Name; PolicyKey = $policyKey; }
    }
}

$Configuration = Get-Configuration

#It is important that we load this library first. If we don't and Azure Powershell loads it's own first, it will load and older version.
Add-Library -LibraryName "Microsoft.ServiceBus.dll" -Location $Configuration.PackagesFolderPath

Export-ModuleMember New-SharedAccessAuthorizationRule