 echo "Creating a local config from template for RunFromConsole projects"
 
 $templateFile = "mysettings-template.config"
 $configFile = "mysettings.config"
 $configFolder = "src\RunFromConsole\"

 $srcPath = Join-Path $configFolder $templateFile
 $dstPath = Join-Path $configFolder $configFile
 
 Copy-Item -Path $srcPath -Destination $dstPath