param([string]$GameRoot='')
. (Join-Path $PSScriptRoot 'environment.ps1')
try {
    $resolvedGameRoot=Get-OC2GameRoot $GameRoot
    $project=Join-Path $projectRoot 'tests\OC2Haptic.Config.Tests\OC2Haptic.Config.Tests.csproj'
    dotnet restore $project --configfile (Join-Path $projectRoot 'NuGet.Config') "-p:GameRoot=$resolvedGameRoot" -p:NuGetAudit=false --verbosity quiet
    if($LASTEXITCODE -ne 0){throw 'Config integration restore failed.'}
    dotnet run --project $project --no-restore --configuration Release "-p:GameRoot=$resolvedGameRoot" -- (Join-Path $projectRoot '.cache\config-tests')
    if($LASTEXITCODE -ne 0){throw 'Config integration tests failed.'}
} finally { Restore-OC2BuildEnvironment }
