[CmdletBinding()]
Param
(
    [ValidateNotNullOrEmpty()]
    [Parameter (Mandatory = $True)]
	[string] $SubscriptionName,

    [ValidateNotNullOrEmpty()]
    [Parameter (Mandatory = $True)]
	[String] $ServerName,

    [ValidateNotNullOrEmpty()]
    [Parameter (Mandatory = $False)]
	[String] $ResourceGroupPrefix = "fabrikam",

    [ValidateNotNullOrEmpty()]
    [Parameter (Mandatory = $True)]
	[String] $ServerAdminLogin,

	[ValidateNotNullOrEmpty()]
    [Parameter (Mandatory = $True)]
	[string] $ServerAdminPassword,

    [ValidateNotNullOrEmpty()]
    [Parameter (Mandatory = $True)]
	[String] $DatabaseName,

    [ValidateNotNullOrEmpty()]
    [Parameter (Mandatory = $False)]
	[String] $Location = "Central US",

    [String] $FirewallRuleName = "DeleteThisRule"
)

.\Init.ps1

$VerbosePreference = "Continue"

Switch-AzureMode -Name AzureServiceManagement

Assert-AzureModuleIsInstalled

# Login to the given subscription
#Add-AzureAccount

Select-AzureSubscription -SubscriptionName $SubscriptionName

Using-AzureResourceManagerMode ({

    $ResourceGroupName = $ResourceGroupPrefix + "-" + $Location.Replace(" ","-")

    New-AzureResourceGroupIfNotExists -ResourceGroupName $ResourceGroupName -Location $Location

    $SqlServer = Get-AzureSqlServer -ServerName $ServerName -ResourceGroupName $ResourceGroupName -EA SilentlyContinue

    if(!$SqlServer)
    {
        Write-Verbose "Creating server"

        $secPassword = ConvertTo-SecureString $ServerAdminPassword -AsPlainText -Force
        $credentials = New-Object System.Management.Automation.PSCredential($ServerAdminLogin, $secPassword)

        $serverContext = New-AzureSqlServer -ResourceGroupName $ResourceGroupName -ServerName $ServerName -SqlAdminCredentials $credentials -location $Location
    
        Write-Verbose ("Created server: {0}" -f $serverContext.ServerName)
    }
    else
    {
        Write-Verbose "A SQL Server named: [$ServerName] already exists."
    }

})

$sqlDatabase = Get-AzureSqlDatabase -ServerName $ServerName -DatabaseName $DatabaseName -EA SilentlyContinue

if(!$sqlDatabase)
{
    Write-Verbose "Creating database"

    New-AzureSqlDatabase -ServerName $ServerName -DatabaseName $DatabaseName

    Write-Verbose ("Created database: {0}" -f $DatabaseName)
}
else
{
    Write-Verbose "A SQL database named: [$DatabaseName] already exists in server [$ServerName]."
}

$firewallRule = Get-AzureSqlDatabaseServerFirewallRule -ServerName $ServerName -RuleName $FirewallRuleName -EA SilentlyContinue

if(!$firewallRule)
{
    Write-Verbose "Creating a dangerous firewall rule"

    $ruleContext = New-AzureSqlDatabaseServerFirewallRule -ServerName $ServerName -RuleName $FirewallRuleName -StartIPAddress "0.0.0.0" -EndIPAddress "255.255.255.255"

    Write-Verbose "Created a dangerous firewall rule"
}
else
{
    Write-Verbose "A Firewall Rule named: [$FirewallRuleName] already exists in server [$ServerName]."
}

$qualifiedServerName = $serverName + ".database.windows.net"

Push-Location .\SqlDatabase

.\ProvisionDatabase.cmd $qualifiedServerName $DatabaseName $ServerAdminLogin $ServerAdminPassword

Pop-Location 