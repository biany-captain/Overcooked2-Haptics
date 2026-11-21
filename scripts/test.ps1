param([string]$Filter = '')
. (Join-Path $PSScriptRoot 'environment.ps1')
try {
$testProject = Join-Path $projectRoot 'tests\OC2Haptic.Core.Tests\OC2Haptic.Core.Tests.csproj'
dotnet restore $testProject --configfile (Join-Path $projectRoot 'NuGet.Config') -p:NuGetAudit=false --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Test project restore failed.' }
$arguments = @('run', '--project', $testProject, '--no-restore', '--configuration', 'Release')
if ($Filter) { $arguments += @('--', $Filter) }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
} finally { Restore-OC2BuildEnvironment }
