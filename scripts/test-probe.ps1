param([string]$GameRoot='')
. (Join-Path $PSScriptRoot 'environment.ps1')
try {
    $resolvedGameRoot=Get-OC2GameRoot $GameRoot
    $project=Join-Path $projectRoot 'tests\OC2Haptic.Probe.Tests\OC2Haptic.Probe.Tests.csproj'
    dotnet restore $project --configfile (Join-Path $projectRoot 'NuGet.Config') "-p:GameRoot=$resolvedGameRoot" -p:NuGetAudit=false --verbosity quiet
    if($LASTEXITCODE -ne 0){throw 'Probe integration restore failed.'}
    dotnet run --project $project --no-restore --configuration Release "-p:GameRoot=$resolvedGameRoot"
    if($LASTEXITCODE -ne 0){throw 'Probe integration tests failed.'}
} finally { Restore-OC2BuildEnvironment }
