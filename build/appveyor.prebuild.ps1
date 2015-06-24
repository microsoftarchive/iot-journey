 echo "Creating a local config from template for projects"
 
 $templateFile1 = "simulator-template.config"
 $configFile1 = "simulator-local.config"
 
 $templateFile2 = "coldstorageeventprocessor-template.config"
 $configFile2 = "coldstorageeventprocessor-local.config"

 $templateFile3 = "warmstorageeventprocessor-template.config"
 $configFile3 = "warmstorageeventprocessor-local.config"

 $configFolder = "provision\config\"

 $srcPath = Join-Path $configFolder $templateFile1
 $dstPath = Join-Path $configFolder $configFile1
 
 Copy-Item -Path $srcPath -Destination $dstPath

 $srcPath = Join-Path $configFolder $templateFile2
 $dstPath = Join-Path $configFolder $configFile2
 
 Copy-Item -Path $srcPath -Destination $dstPath

 $srcPath = Join-Path $configFolder $templateFile3
 $dstPath = Join-Path $configFolder $configFile3
 
 Copy-Item -Path $srcPath -Destination $dstPath