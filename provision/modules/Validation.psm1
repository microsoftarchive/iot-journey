function Test-OnlyLettersAndNumbers
{
    
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$True)][string]$Name,
        [Parameter(Mandatory=$True)][string]$Value
    )
    PROCESS
    {
        # needs contain only lower case letters and numbers.
        If ($Value -cmatch "^[a-z0-9]*$") 
        {
            $True
        }
        else 
        {
            Throw "`n ---> [$Name] can only contain lowercase letters and numbers! <---"
        }
    }
}

function Test-OnlyLettersNumbersAndHyphens
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$True)][string]$Name,
        [Parameter(Mandatory=$True)][string]$Value
    )
    PROCESS
    {
        # needs to start with letter or number, and contain only letters, numbers, and hyphens.
        If ($Value -cmatch "^[A-Za-z][-A-Za-z0-9]*[A-Za-z0-9]$") 
        {
            $True
        }
        else 
        {
            Throw "`n ---> [$Name] needs to start with letter or number, and contain only letters, numbers, and hyphens! <---"
        }
    }
}

function Test-OnlyLettersNumbersHyphensPeriodsAndUnderscores
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$True)][string]$Name,
        [Parameter(Mandatory=$True)][string]$Value
    )
    PROCESS
    {
        # needs to start with letter or number, and contain only letters, numbers, periods, hyphens, and underscores.
        If ($Value -cmatch "^[A-Za-z0-9]$|^[A-Za-z0-9][\w-\.\/]*[A-Za-z0-9]$") 
        {
            $True
        }
        else 
        {
            Throw "`n ---> [$Name] needs to start with letter or number, and contain only letters, numbers, periods, hyphens, and underscores! <---"
        }
    }
}

function Test-FileName
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$True)][string]$Name,
        [Parameter(Mandatory=$True)][string]$Value
    )
    PROCESS
    {
        If ($Value -cmatch "^[A-Za-z0-9]$|^[A-Za-z0-9][\w-\.\/]*[A-Za-z0-9]$") 
        {
            $True
        }
        else 
        {
            Throw "`n ---> [$Name] invalid file name! <---"
        }
    }
}

Export-ModuleMember Test-OnlyLettersAndNumbers
Export-ModuleMember Test-OnlyLettersNumbersAndHyphens
Export-ModuleMember Test-OnlyLettersNumbersHyphensPeriodsAndUnderscores
Export-ModuleMember Test-FileName