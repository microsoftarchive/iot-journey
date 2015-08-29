# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<#
.SYNOPSIS
  Deploy long term storage with Azure Stream Analytics example.
#>
[CmdletBinding()]
Param
(
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][string]$SubscriptionName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $True)][String]$ApplicationName,
	[ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$StorageAccountName =$ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ServiceBusNamespace = $ApplicationName,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$EventHubName = "eventhub-iot",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$ConsumerGroupName  = "cg-blobs-asa",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][string]$ResourceGroupName = "IoTJourney",
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][bool]$AddAccount =$True,
    [ValidateNotNullOrEmpty()][Parameter (Mandatory = $False)][String]$Location = "Central US"
)
PROCESS
{
    .\..\..\..\..\Init.ps1

    Load-Module -ModuleName Validation -ModuleLocation .\..\..\..\..\modules

    #TODO: validate input parameters

	Load-Module -ModuleName Config -ModuleLocation .\..\..\..\..\modules
    Load-Module -ModuleName AzureARM -ModuleLocation .\..\..\..\..\modules
	Load-Module -ModuleName AzureServiceBus -ModuleLocation .\..\..\..\..\modules

    if($AddAccount)
    {
        Add-AzureAccount
    }

    Select-AzureSubscription $SubscriptionName

    Invoke-InAzureResourceManagerMode ({

        New-AzureResourceGroupIfNotExists -ResourceGroupName $ResourceGroupName -Location $Location

        New-AzureResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
                                         -TemplateFile .\Templates\EventHub.json `
                                         -TemplateParameterObject @{ namespaceName = $ServiceBusNamespace; eventHubName=$EventHubName; consumerGroupName=$ConsumerGroupName }

    })

	Assert-ServiceBusDll

	#New-EventHubSharedAccessAuthorizationRule
	
	$sbr = Get-AzureSBAuthorizationRule -Namespace $ServiceBusNamespace
	$NamespaceManager = [Microsoft.ServiceBus.NamespaceManager]::CreateFromConnectionString($sbr.ConnectionString); 


	$EventHubDescription = $NamespaceManager.GetEventHub($EventHubName)
	
	$Key = [Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule]::GenerateRandomKey()
	$Rights = [Microsoft.ServiceBus.Messaging.AccessRights]::Listen, [Microsoft.ServiceBus.Messaging.AccessRights]::Send
	$RightsColl = New-Object -TypeName System.Collections.Generic.List[Microsoft.ServiceBus.Messaging.AccessRights] (,[Microsoft.ServiceBus.Messaging.AccessRights[]]$Rights)
	$AccessRule = New-Object -TypeName  Microsoft.ServiceBus.Messaging.SharedAccessAuthorizationRule -ArgumentList "SendReceive", $Key, $RightsColl
	$EventHubDescription.Authorization.Add($AccessRule)

	$NamespaceManager.UpdateEventHub($EventHubDescription)

	$StreamAnalyticsJobName = $ApplicationName+"ToBlob"
	Invoke-InAzureResourceManagerMode ({

		New-AzureResourceGroupDeployment -ResourceGroupName $ResourceGroupName `
                                         -TemplateFile .\Templates\StreamAnalytics.json `
                                         -TemplateParameterObject @{ 
											 jobName = $StreamAnalyticsJobName;
											 storageAccountName=$StorageAccountName;
											 consumerGroupName=$ConsumerGroupName;
											 eventHubName=$EventHubName;
											 serviceBusNamespace=$ServiceBusNamespace;
											 sharedAccessPolicyName="SendReceive";
											 sharedAccessPolicyKey=$Key;
										 }
			
	})
}