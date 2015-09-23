    #simulator settings
    $simulatorSettings = @{
        'Simulator.EventHubNamespace'= $deployInfo.Outputs["serviceBusNamespaceName"].Value;
        'Simulator.EventHubName' = $deployInfo.Outputs["eventHubName"].Value;
        'Simulator.EventHubSasKeyName' = $deployInfo.Outputs["sharedAccessPolicyName"].Value;
        'Simulator.EventHubPrimaryKey' = $deployInfo.Outputs["sharedAccessPolicyPrimaryKey"].Value;
        'Simulator.EventHubTokenLifetimeDays' = ($deployInfo.Outputs["messageRetentionInDays"].Value -as [string]);
    }
    
    Write-SettingsFile -configurationTemplateFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\src\Simulator\ScenarioSimulator.ConsoleHost.Template.config") `
                       -configurationFile (Join-Path $PSScriptRoot -ChildPath "..\..\..\..\src\Simulator\ScenarioSimulator.ConsoleHost.config") `
                       -appSettings $simulatorSettings