function Get-Configuration
{
    #All paths are relative to modules folder.
    @{
        "PackagesFolderPath" = Join-Path -Path $PSScriptRoot -ChildPath "..\..\src\packages";
        "UtilityFolderPath" = Join-Path -Path $PSScriptRoot -ChildPath "..\utility";
        "SourceFolderPath" = Join-Path -Path $PSScriptRoot -ChildPath "..\..\src";
    }
}

Export-ModuleMember Get-Configuration