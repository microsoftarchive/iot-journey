# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

function Get-Configuration
{
    #All paths are relative to modules folder.
    @{
        "PackagesFolderPath" = Join-Path -Path $PSScriptRoot -ChildPath "..\..\src\packages";
        "SourceFolderPath" = Join-Path -Path $PSScriptRoot -ChildPath "..\..\src";
    }
}

Export-ModuleMember Get-Configuration