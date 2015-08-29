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

Assert-AzurePowershellVersion "0.9.7"