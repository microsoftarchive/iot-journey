############################
##
## Azure Stream Analytics
##
############################

Load-Module -ModuleName Validation -ModuleLocation .\modules
Load-Module -ModuleName AzureStorage -ModuleLocation .\modules
Load-Module -ModuleName AzureServiceBus -ModuleLocation .\modules

function Provision-StreamAnalyticsJob
{
    [CmdletBinding()]
    param
    (
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ServiceBusNamespace,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$EventHubName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$EventHubSharedAccessPolicyName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$StorageAccountName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ContainerName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ResourceGroupName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$Location,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$JobDefinitionText
    )
    PROCESS
    {
        Assert-ServiceBusDll

        Invoke-InAzureResourceManagerMode ({
            
            New-AzureResourceGroupIfNotExists -ResourceGroupName $ResourceGroupName -Location $Location

            New-StreamAnalyticsJobFromDefinition -JobDefinitionText $JobDefinitionText `
                                                 -ResourceGroupName $ResourceGroupName

            Write-Verbose "Completed created Azure Stream Analytics Job"
                        
        })

        Write-Verbose "Create Azure StreamAnalyticsJob Completed"
    }
}

# private

function New-StreamAnalyticsJobFromDefinition
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$True)][object]$JobDefinitionText,
        [Parameter(Mandatory=$True)][string]$ResourceGroupName
    )
    PROCESS
    {
        $TempFileName = [guid]::NewGuid().ToString() + ".json"
        $JobDefinitionText > $TempFileName

        try
        {
            New-AzureStreamAnalyticsJob -ResourceGroupName $ResourceGroupName  -File $TempFileName -Force
        }
        finally
        {
            if (Test-Path $TempFileName) 
            {
                Write-Verbose "Deleting the temp file ... "
                Clear-Content $TempFileName
                Remove-Item $TempFileName
            }
        }
    }
}

Export-ModuleMember Provision-StreamAnalyticsJob
