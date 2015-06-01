############################
##
## Helper functions
##
############################
function Load-Module
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

##
# script initialization
##
Load-Module -ModuleName AzureUtilities -ModuleLocation .\Modules