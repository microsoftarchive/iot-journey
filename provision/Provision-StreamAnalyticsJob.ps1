<# 
    .SYNOPSIS 
    This script is to be used to provision the stream analytics job that is defined 
    in StreamAnalyticsJobDefinition.json
             
    .DESCRIPTION 
    This script is to be used to provision the stream analytics job that is defined 
    in StreamAnalyticsJobDefinition.json

    In particular, the script allows to specify the following parameters: 
    -- The Azure Subscription Id
    -- The SharedAccessPolicyKey of the input event hub
    -- The StorageAccountKey for the output blob
    -- The Path to job definition json file of the stream analytics
    
    .PARAMETER  AzureSubscriptionId 
    Specifies The Azure Subscription Id

    .PARAMETER  EventHubSharedAccessPolicyKey 
    Specifies The SharedAccessPolicyKey of the input event hub

    .PARAMETER  BlobStorageAccountKey 
    Specifies The StorageAccountKey for the output blob

    .PARAMETER  Path 
    Specifies the full path to job definition json file of the stream analytics
 
    .NOTES   
    Author     : Hanz Zhang
#> 


[CmdletBinding(PositionalBinding=$True)] 
Param( 
    [Parameter(Mandatory = $true)] 
    [String]$AzureSubscriptionId,                                          # required

    [Parameter(Mandatory = $true)] 
    [String]$EventHubSharedAccessPolicyKey,                                # required

    [Parameter(Mandatory = $true)] 
    [String]$BlobStorageAccountKey,                                        # required

    [ValidatePattern("^[A-Za-z0-9]$|^[A-Za-z0-9][\w-\.\/]*[A-Za-z0-9]$")] 
    [String]$JobDefinitionPath = ".\StreamAnalyticsJobDefinition.json"    # optional default to C:\JobDefinition.json
    ) 
        
$EventHubSharedAccessPolicyKeyPlaceHolder = "EventHubSharedAccessPolicyKeyPlaceHolder"
$BlobStorageAccountKeyPlacHolder = "BlobStorageAccountKeyPlacHolder"
$MemoryMappedFilePath = [guid]::NewGuid().ToString() + ".json"

Add-AzureAccount 
Select-AzureSubscription –SubscriptionId $AzureSubscriptionId
Switch-AzureMode AzureResourceManager

$JobDefinitionText = get-content $JobDefinitionPath 
$JobDefinitionText = $JobDefinitionText -replace $EventHubSharedAccessPolicyKeyPlaceHolder,$EventHubSharedAccessPolicyKey
$JobDefinitionText = $JobDefinitionText -replace $BlobStorageAccountKeyPlacHolder,$BlobStorageAccountKey

[System.Reflection.Assembly]::LoadWithPartialName("System.IO.MemoryMappedFiles") 
$MemMappedFile = [System.IO.MemoryMappedFiles.MemoryMappedFile]::CreateNew([string]$MemoryMappedFilePath,$JobDefinitionText.Length)
$JobDefinitionText > $MemoryMappedFilePath

New-AzureStreamAnalyticsJob -ResourceGroupName StreamAnalytics-Default-West-US  -File $MemoryMappedFilePath -Force

$MemMappedFile.Dispose()
$MemMappedFile.SafeMemoryMappedFileHandle.Close()

Write-Output "Create Azure StreamAnalyticsJob Completed"
