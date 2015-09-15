# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

#Executes an script in ARM and inmediatly sets ASM back.
function Invoke-InAzureResourceManagerMode
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$True)][scriptblock]$ScriptBlock
    )
    PROCESS
    {
        try
        {
            Switch-AzureMode -Name AzureResourceManager -WarningAction SilentlyContinue

            . $ScriptBlock
        }
        finally
        {
            Switch-AzureMode -Name AzureServiceManagement -WarningAction SilentlyContinue
        }
    }
}

function New-AzureResourceGroupIfNotExists
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$True)][string]$ResourceGroupName,
        [Parameter(Mandatory=$True)][string]$Location
    )
    PROCESS
    {
        if(!(Get-AzureResourceGroup -Name $ResourceGroupName -EA SilentlyContinue))
        {
            Write-Verbose "The [$ResourceGroupName] ResourceGroup does not exist." 
            Write-Verbose "Creating the [$ResourceGroupName] ResourceGroup..." 
            
            New-AzureResourceGroup -Name $ResourceGroupName -Location $Location

            Write-Verbose "The [$ResourceGroupName] Resource Group in the [$Location] region has been successfully created."
        }
        else
        {
            Write-Verbose "The ResourceGroup [$ResourceGroupName] already exists"
        }
    }
}

Export-ModuleMember Invoke-InAzureResourceManagerMode
Export-ModuleMember New-AzureResourceGroupIfNotExists