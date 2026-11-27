param([string]$GameRoot = '')
. (Join-Path $PSScriptRoot 'environment.ps1')
try {
& (Join-Path $PSScriptRoot 'generate-haptic-preset.ps1') -Check
$resolvedGameRoot = Get-OC2GameRoot $GameRoot
$pluginProject = Join-Path $projectRoot 'src\OC2Haptic.Plugin\OC2Haptic.Plugin.csproj'
dotnet restore $pluginProject --configfile (Join-Path $projectRoot 'NuGet.Config') "-p:GameRoot=$resolvedGameRoot" -p:NuGetAudit=false --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Plugin restore failed.' }
dotnet build $pluginProject --no-restore --configuration Release "-p:GameRoot=$resolvedGameRoot" --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
$artifactRoot = Join-Path $projectRoot 'artifacts'
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\OC2Haptic.Plugin\bin\Release\net35\OC2Haptic.dll') -Destination (Join-Path $artifactRoot 'OC2Haptic.dll') -Force
Write-Output "Plugin artifact: $artifactRoot\OC2Haptic.dll"
} finally { Restore-OC2BuildEnvironment }
