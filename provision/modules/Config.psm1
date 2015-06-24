function Get-Configuration
{
    @{
        "PackagesFolderPath" = Join-Path -Path $PSScriptRoot -ChildPath "..\..\src\packages";
        "UtilityFolderPath" = Join-Path $PSScriptRoot -ChildPath "..\utility"
    }
}

Export-ModuleMember Get-Configuration