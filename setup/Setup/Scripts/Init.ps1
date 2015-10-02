# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

############################
##
## Helper functions
##
############################

function global:Load-Module
{
    Param (
        $ModuleName,
        $ModuleLocation
    )
    if (Get-Module -Name $ModuleName -Verbose:$False -EA Stop)
    {
        Remove-Module -Name $ModuleName -Verbose:$False -EA Stop
    }
    $QualifiedModuleName = $ModuleLocation + "\" + $ModuleName
    $Ignore = Import-Module -Name $QualifiedModuleName -PassThru -Verbose:$False -EA Stop
}

function global:Add-Library
{
    [CmdletBinding()]
    param 
    (
        [Parameter(Mandatory=$True)][string]$LibraryName,
        [Parameter(Mandatory=$True)][string]$Location
    )
    PROCESS
    {
        try
        {
            Write-Output "Adding the [$LibraryName] assembly to the script..." 

            $Assembly = Get-ChildItem $Location -Include $LibraryName -Recurse
            Add-Type -Path $Assembly.FullName

            Write-Output "The [$LibraryName] assembly has been successfully added to the script." 
        }
        catch
        {
            Write-Error "Could not add the [$LibraryName] assembly to the script. Make sure you build the solution before running the provisioning script."
            Break
        }
    }
   
}

function Assert-AzurePowershellVersion
{
        Param ([version]$requiredVersion)

        if (-Not (Get-Module -ListAvailable -Name Azure)) {
            Throw "Azure Powershell SDK is not installed." 
        }

        if (-Not (Get-Module Azure)) {
            Import-Module Azure
        }

        $version = (Get-Module Azure).Version
        Write-Host "Azure Powershell SDK Version $($version) is installed."

        if ($version -lt $requiredVersion) {
            Throw "This script requires at least version $($requiredVersion) of the Azure Powershell SDK."
        }
}

############################
##
## Script start up
##
############################

$PSDefaultParameterValues = $PSDefaultParameterValues.clone()
$PSDefaultParameterValues += @{'New-RegKey:ErrorAction' = 'Stop'}

Assert-AzurePowershellVersion "0.9.8"