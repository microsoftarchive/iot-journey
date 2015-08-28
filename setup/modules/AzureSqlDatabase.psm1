############################
##
## Azure Sql Database
##
############################

function Provision-SqlDatabase
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$True)][string]$SqlServerName,
        [Parameter(Mandatory=$True)][string]$SqlServerAdminLogin,
        [Parameter(Mandatory=$True)][string]$SqlServerAdminPassword,
        [Parameter(Mandatory=$True)][string]$SqlDatabaseName,
        [Parameter(Mandatory=$True)][string]$ResourceGroupName,
        [Parameter(Mandatory=$True)][string]$Location
    )
    PROCESS
    {
        Invoke-InAzureResourceManagerMode ({
            
            New-AzureResourceGroupIfNotExists -ResourceGroupName $ResourceGroupName -Location $Location

            New-SqlServerIfNotExists -SqlServerName $SqlServerName `
                                     -ResourceGroupName $ResourceGroupName `
                                     -SqlServerAdminLogin $SqlServerAdminLogin `
                                     -SqlServerAdminPassword $SqlServerAdminPassword
        })

        New-SqlDatabaseIfNotExists -SqlServerName $SqlServerName `
                                   -SqlDatabaseName $SqlDatabaseName

        New-SqlFirewallRuleIfNotExists -SqlServerName $SqlServerName `
                                       -FirewallRuleName "DeleteThisRule" `
                                       -StartIPAddress "0.0.0.0" `
                                       -EndIPAddress "255.255.255.255"

        $QualifiedSqlServerName = $SqlServerName + ".database.windows.net"

        $Configuration = Get-Configuration

		$schemaFilePath = Join-Path $PSScriptRoot -ChildPath "..\data\CreateSqlDatabase_Schema.sql"

		Invoke-SqlScript -SqlServerName $QualifiedSqlServerName `
						 -SqlDatabaseName $SqlDatabaseName `
						 -SqlServerAdminLogin $SqlServerAdminLogin `
						 -SqlServerAdminPassword $SqlServerAdminPassword `
						 -ScriptFileName $schemaFilePath
    }
}

# private

function Invoke-SqlScript
{
	[CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$True)][string]$SqlServerName,
        [Parameter(Mandatory=$True)][string]$SqlDatabaseName,
        [Parameter(Mandatory=$True)][string]$SqlServerAdminLogin,
        [Parameter(Mandatory=$True)][string]$SqlServerAdminPassword,
		[Parameter(Mandatory=$True)][string]$ScriptFileName,
		[Parameter(Mandatory=$false)][bool]$IntegratedSecurity = $false
    )
    PROCESS
    {
		$connStrBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
		$connStrBuilder["Integrated Security"] = $IntegratedSecurity
		$connStrBuilder["Data Source"] = $SqlServerName
		$connStrBuilder["Initial Catalog"] = $SqlDatabaseName
		$connStrBuilder["User ID"] = $SqlServerAdminLogin
		$connStrBuilder["Password"] = $SqlServerAdminPassword

		$connection = New-Object System.Data.SqlClient.SqlConnection($connStrBuilder.ConnectionString)
		$command = $connection.CreateCommand()

		$builder = New-Object System.Text.StringBuilder
		[int]$num = 0
        
        $connection.Open()

		$reader = [System.IO.File]::OpenText($ScriptFileName)
		try
		{
			for(;;) 
			{
				$line = $reader.ReadLine()
				$num++
				if (($line -eq $null) -or $line.ToUpper().Trim().Equals("GO"))
				{
					if($builder.Length -gt 0) 
					{
						$command.CommandText = $builder.ToString()
						try
						{
							$command.ExecuteNonQuery() | out-null
						}
						catch
						{
							$errorMessage = $_.Exception.Message
							Write-Error "Error at or before line $num : $errorMessage"
						}
                        
						$builder.Length = 0
					}
                    if($line -eq $null)
                    {
                        break
                    }
				}
				else
				{
					$builder.AppendLine($line)
				}
			}
		}
		finally 
		{
			$reader.Close()
			$connection.Close()
		}
	}
}

function New-SqlServerIfNotExists
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$True)][string]$SqlServerName,
        [Parameter(Mandatory=$True)][string]$ResourceGroupName,
        [Parameter(Mandatory=$True)][string]$SqlServerAdminLogin,
        [Parameter(Mandatory=$True)][string]$SqlServerAdminPassword
    )
    PROCESS
    {
        if (!(Get-AzureSqlServer -ServerName $SqlServerName -ResourceGroupName $ResourceGroupName -EA SilentlyContinue))
        {
            Write-Verbose "Creating server"

            $secPassword = ConvertTo-SecureString $SqlServerAdminPassword -AsPlainText -Force
            $credentials = New-Object System.Management.Automation.PSCredential($SqlServerAdminLogin, $secPassword)

            $serverContext = New-AzureSqlServer -ResourceGroupName $ResourceGroupName -ServerName $SqlServerName -SqlAdministratorCredentials $credentials -location $Location
    
            Write-Verbose ("Created server: {0}" -f $serverContext.ServerName)

            return
        }

        Write-Verbose "A SQL Server named: [$SqlServerName] already exists."

        return
    }
}

function New-SqlDatabaseIfNotExists
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$True)][string]$SqlServerName,
        [Parameter(Mandatory=$True)][string]$SqlDatabaseName
    )
    PROCESS
    {
        if (!(Get-AzureSqlDatabase -ServerName $SqlServerName -DatabaseName $SqlDatabaseName -EA SilentlyContinue))
        {
            Write-Verbose "Creating database"

            New-AzureSqlDatabase -ServerName $SqlServerName -DatabaseName $SqlDatabaseName

            Write-Verbose ("Created database: {0}" -f $SqlDatabaseName)

            return
        }

        Write-Verbose "A SQL database named: [$SqlDatabaseName] already exists in server [$SqlServerName]."

        return
    }
}

function New-SqlFirewallRuleIfNotExists
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$True)][string]$SqlServerName,
        [Parameter(Mandatory=$True)][string]$FirewallRuleName,
        [Parameter(Mandatory=$True)][string]$StartIPAddress,
        [Parameter(Mandatory=$True)][string]$EndIPAddress
    )
    PROCESS
    {
        if (!(Get-AzureSqlDatabaseServerFirewallRule -ServerName $SqlServerName -RuleName $FirewallRuleName -EA SilentlyContinue))
        {
           Write-Verbose "Creating a dangerous firewall rule"

            $ruleContext = New-AzureSqlDatabaseServerFirewallRule -ServerName $SqlServerName -RuleName $FirewallRuleName -StartIPAddress $StartIPAddress -EndIPAddress $EndIPAddress

            Write-Verbose "Created a dangerous firewall rule"

            return
        }

        Write-Verbose "A Firewall Rule named: [$FirewallRuleName] already exists in server [$SqlServerName]."

        return
    }
}

Export-ModuleMember Provision-SqlDatabase