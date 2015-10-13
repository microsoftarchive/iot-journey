function New-StorageContainerIfNotExists
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$True)][string]$ContainerName,
        [Parameter(Mandatory=$True)][object]$Context
    )
    PROCESS
    {
        if (!(Get-AzureStorageContainer -Name $ContainerName -ErrorAction SilentlyContinue -Context $context))
        {
            New-AzureStorageContainer -Context $context -Name $ContainerName

            return
        }

        Write-Verbose ("The storage container {0} already exists." -f $ContainerName)

        return
    }
}

Export-ModuleMember New-StorageContainerIfNotExists