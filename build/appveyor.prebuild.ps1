 echo "Creating a config from template for projects"

 $srcPath = "src\Simulator\ScenarioSimulator.ConsoleHost\ScenarioSimulator.ConsoleHost.Template.config"
 $dstPath = "src\Simulator\ScenarioSimulator.ConsoleHost\ScenarioSimulator.ConsoleHost.config" 
 Copy-Item -Path $srcPath -Destination $dstPath

 $srcPath = "src\AdhocExploration\DotnetEventProcessor\WarmStorage.EventProcessor.ConsoleHost\WarmStorage.EventProcessor.ConsoleHost.Template.config"
 $dstPath = "src\AdhocExploration\DotnetEventProcessor\WarmStorage.EventProcessor.ConsoleHost\WarmStorage.EventProcessor.ConsoleHost.config"
 Copy-Item -Path $srcPath -Destination $dstPath

 $srcPath = "src\LongTermStorage\DotnetEventProcessor\ColdStorage.EventProcessor.ConsoleHost.Template.config"
 $dstPath = "src\LongTermStorage\DotnetEventProcessor\ColdStorage.EventProcessor.ConsoleHost.config"
 Copy-Item -Path $srcPath -Destination $dstPath