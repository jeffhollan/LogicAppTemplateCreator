param([string] $version, [string] $apikey)

$binPath = Join-Path $PSScriptRoot "bin\Release"
$modulePath = Join-Path $PSScriptRoot "bin\LogicAppTemplate"

$manifestPath = Join-Path $PSScriptRoot "LogicAppTemplate.psd1"
$manifest = Test-ModuleManifest -Path $manifestPath

Update-ModuleManifest -Path $manifestPath -CmdletsToExport '*' -ModuleVersion $version

Write-Host "Preparing module"

New-Item $modulePath -ItemType Directory -Force | Out-Null
Copy-Item (Join-Path $binPath "*.dll") $modulePath
Copy-Item (Join-Path $PSScriptRoot "LogicAppTemplate.psd1") (Join-Path $modulePath "LogicAppTemplate.psd1")

Write-Host "Publishing module"

Publish-Module -Path $modulePath -Repository PSGallery -NuGetApiKey $apikey

Remove-Item $modulePath -Force -Recurse
