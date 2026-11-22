param([string]$GameRoot = '', [switch]$SkipBuild)
. (Join-Path $PSScriptRoot 'environment.ps1')
try {
if (!$SkipBuild) { & (Join-Path $PSScriptRoot 'build.ps1') -GameRoot $GameRoot }
$artifactRoot = Join-Path $projectRoot 'artifacts'
$dll = Join-Path $artifactRoot 'OC2Haptic.dll'
if (!(Test-Path -LiteralPath $dll)) { throw 'Build OC2Haptic.dll first.' }
$version = [Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString(3) + '-alpha'
$staging = Join-Path $artifactRoot ('package-' + [guid]::NewGuid().ToString('N'))
$pluginDir = Join-Path $staging 'BepInEx\plugins\OC2Haptic'
$configDir = Join-Path $staging 'config'
New-Item -ItemType Directory -Path $pluginDir, $configDir -Force | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $pluginDir 'OC2Haptic.dll')
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets\audio') -Destination (Join-Path $pluginDir 'audio') -Recurse
Copy-Item -LiteralPath (Join-Path $projectRoot 'config\OC2Haptic.cfg.example') -Destination $configDir
Copy-Item -LiteralPath (Join-Path $projectRoot 'config\haptic_patterns_v1.6.json') -Destination $configDir
Copy-Item -LiteralPath (Join-Path $projectRoot 'config\star_feedback_v1.7.json') -Destination $configDir
foreach ($file in @('README.md', 'CHANGELOG.md')) { Copy-Item -LiteralPath (Join-Path $projectRoot $file) -Destination $staging }
$docsDir = Join-Path $staging 'docs'
New-Item -ItemType Directory -Path $docsDir -Force | Out-Null
foreach ($file in @('test-report.md','test-report-0.1.md','test-report-0.2.md','test-report-0.2.1.md','test-report-0.2.2.md','test-report-0.2.3.md','test-report-0.3.md','test-report-0.3.1.md','hook-research.md','compatibility-matrix.md','decisions.md','review-ab-alpha.md','review-stage-c.md','review-menu.md','review-score-probe.md','review-v13.md','batch-test-0.2.md','batch-test-0.3.md','batch-test-0.4.md','audio-packs.md','menu-test-0.2.1.md','menu-layout-0.2.2.md','design-v1.3.md','design-v1.4.md','calibration-0.2.3.md','requirements-v1.0.md','requirements-v1.3.md','requirements-v1.4.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot ('docs\' + $file)) -Destination $docsDir
}
$evidenceDir = Join-Path $projectRoot 'docs\evidence'
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\roadmap.md') -Destination $docsDir
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\review-v14.md') -Destination $docsDir
foreach($file in @('test-report-0.4.md','test-report-0.4.1.md','design-ultra-v3.md','batch-test-0.4.1.md','review-ultra-v3.md',
    'design-simple-controls.md','batch-test-0.4.2.md','review-simple-controls.md','test-report-0.4.2.md',
    'haptic-redesign-v1.6.md','codex-v1.6-incremental.md','audit-v1.6.md','design-v1.6.md','backend-extension.md','batch-test-v1.6.md','review-v1.6.md',
    'test-report-0.5.md','design-feature-switches.md','batch-test-0.5.1.md','review-feature-switches.md',
    'test-report-0.5.1.md','requirements-star-feedback-v1.7.md','codex-star-feedback-v1.7.md','audit-star-feedback-v1.7.md','design-star-feedback-v1.7.md','batch-test-v1.7.md','review-star-feedback-v1.7.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot ('docs\'+$file)) -Destination $docsDir
}
if (Test-Path -LiteralPath $evidenceDir) { Copy-Item -LiteralPath $evidenceDir -Destination (Join-Path $docsDir 'evidence') -Recurse }
$runtimeZip = Join-Path $artifactRoot "OC2Haptic-$version.zip"
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $runtimeZip -Force

$sourceStage = Join-Path $artifactRoot ('source-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $sourceStage -Force | Out-Null
$sourceFiles = @()
foreach ($folder in @('src','tests','scripts','docs','config','assets')) {
    $sourceFiles += Get-ChildItem -LiteralPath (Join-Path $projectRoot $folder) -Recurse -File |
        Where-Object { $_.FullName -notmatch '\\(bin|obj|__pycache__|superpowers)\\' -and $_.Name -ne 'progress.md' -and $_.Extension -notin @('.dll','.pdb','.log','.pyc') }
}
foreach ($file in @('README.md','CHANGELOG.md','NuGet.Config','.gitignore','.gitattributes','OC2Haptic.sln')) {
    if (Test-Path -LiteralPath (Join-Path $projectRoot $file)) { $sourceFiles += Get-Item -LiteralPath (Join-Path $projectRoot $file) }
}
foreach ($file in $sourceFiles) {
    $relativePath = $file.FullName.Substring($projectRoot.Length + 1)
    $destination = Join-Path $sourceStage $relativePath
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination
}
$sourceZip = Join-Path $artifactRoot "OC2Haptic-$version-source.zip"
Compress-Archive -Path (Join-Path $sourceStage '*') -DestinationPath $sourceZip -Force
Write-Output "Runtime ZIP: $runtimeZip"
Write-Output "Source ZIP: $sourceZip"
} finally { Restore-OC2BuildEnvironment }
