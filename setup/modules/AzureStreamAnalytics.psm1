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
	    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$BlobsConsumerGroupName,
	    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$SqlConsumerGroupName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$EventHubSharedAccessPolicyName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$StorageAccountName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ContainerName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$SqlDatabaseName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$SqlDatabaseLoginPassword,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$SqlServerName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$SqlDatabaseLogin,

        [ValidateNotNullOrEmpty()]
        [Parameter (Mandatory = $True)]
        [ValidateScript({ Test-FileName "JobDefinitionPathSQL" $_})]
        [String]$JobDefinitionPathSQL,

        [ValidateNotNullOrEmpty()]
        [Parameter (Mandatory = $True)]
        [ValidateScript({ Test-FileName "JobDefinitionPathCold" $_})]
        [String]$JobDefinitionPathCold,

        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$StreamAnalyticsSQLJobName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$StreamAnalyticsBlobsJobName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ResourceGroupName,
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$Location
    )
    PROCESS
    {
        Assert-ServiceBusDll

        $EventHubSharedAccessPolicyKey = Get-EventHubSharedAccessPolicyKey -ServiceBusNamespace $ServiceBusNamespace `
                                                                           -EventHubName $EventHubName `
                                                                           -EventHubSharedAccessPolicyName $EventHubSharedAccessPolicyName

        # Get Storage Account Key
        $storageAccountKey = Get-AzureStorageKey -StorageAccountName $StorageAccountName
        $storageAccountKeyPrimary = $storageAccountKey.Primary
        $RefdataContainerName = $ContainerName + "-refdata"
        
        $context = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $storageAccountKeyPrimary;

        New-StorageContainerIfNotExists -ContainerName $RefdataContainerName `
                                        -Context $context

        Upload-ReferenceData -ContainerName $RefdataContainerName

        # Create SQL Job Definition
        $SqlJobDefinitionText = (Get-Content -LiteralPath $JobDefinitionPathSQL).
                                    Replace("_StreamAnalyticsJobName",$StreamAnalyticsSQLJobName).
                                    Replace("_Location",$Location).
                                    Replace("_ConsumerGroupName",$SqlConsumerGroupName).
                                    Replace("_EventHubName",$EventHubName).
                                    Replace("_ServiceBusNamespace",$ServiceBusNamespace).
                                    Replace("_EventHubSharedAccessPolicyName",$EventHubSharedAccessPolicyName).
                                    Replace("_EventHubSharedAccessPolicyKey",$EventHubSharedAccessPolicyKey).
                                    Replace("_AccountName",$StorageAccountName).
                                    Replace("_AccountKey",$storageAccountKeyPrimary).
                                    Replace("_Container",$ContainerName).
                                    Replace("_RefdataContainer",$RefdataContainerName).
                                    Replace("_DBName",$SqlDatabaseName).
                                    Replace("_DBPassword",$SqlDatabaseLoginPassword).
                                    Replace("_DBServer",$SqlServerName).
                                    Replace("_DBUser",$SqlDatabaseLogin)

        # Create Blobs Job Definition
        $BlobsJobDefinitionText = (Get-Content -LiteralPath $JobDefinitionPathCold).
                                        Replace("_StreamAnalyticsJobName",$StreamAnalyticsBlobsJobName).
                                        Replace("_Location",$Location).
                                        Replace("_ConsumerGroupName",$BlobsConsumerGroupName).
                                        Replace("_EventHubName",$EventHubName).
                                        Replace("_ServiceBusNamespace",$ServiceBusNamespace).
                                        Replace("_EventHubSharedAccessPolicyName",$EventHubSharedAccessPolicyName).
                                        Replace("_EventHubSharedAccessPolicyKey",$EventHubSharedAccessPolicyKey).
                                        Replace("_AccountName",$StorageAccountName).
                                        Replace("_AccountKey",$storageAccountKeyPrimary).
                                        Replace("_Container",$ContainerName).
                                        Replace("_RefdataContainer",$RefdataContainerName).
                                        Replace("_DBName",$SqlDatabaseName).
                                        Replace("_DBPassword",$SqlDatabaseLoginPassword).
                                        Replace("_DBServer",$SqlServerName).
                                        Replace("_DBUser",$SqlDatabaseLogin)

        Using-AzureResourceManagerMode ({
            
            New-AzureResourceGroupIfNotExists -ResourceGroupName $ResourceGroupName -Location $Location

            New-StreamAnalyticsJobFromDefinition -JobDefinitionText $SqlJobDefinitionText `
                                                 -ResourceGroupName $ResourceGroupName

            Write-Verbose "Completed created Azure Stream Analytics Job for SQL"

            New-StreamAnalyticsJobFromDefinition -JobDefinitionText $BlobsJobDefinitionText `
                                                 -ResourceGroupName $ResourceGroupName

            Write-Verbose "Create Azure Stream Analytics Job for Cold Storage"
            
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

function Upload-ReferenceData
{
    [CmdletBinding()]
    param
    (
        [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ContainerName
    )
    PROCESS
    {
        $key = Get-AzureStorageKey -StorageAccountName $StorageAccountName
        $context = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $key.Primary;
        Set-AzureStorageBlobContent -Blob fabrikam/buildingdevice.json -Container $ContainerName -File .\fabrikam_buildingdevice.json -Context $context -Force
    }
}

Export-ModuleMember Provision-StreamAnalyticsJob