$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$oc2SavedEnvironment = @{}
foreach ($name in @('DOTNET_CLI_TELEMETRY_OPTOUT','DOTNET_SKIP_FIRST_TIME_EXPERIENCE','DOTNET_GENERATE_ASPNET_CERTIFICATE','DOTNET_ADD_GLOBAL_TOOLS_TO_PATH','APPDATA','NUGET_PACKAGES')) {
    $oc2SavedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
# Scoped to this PowerShell build process; isolate NuGet from inaccessible account settings.
$env:APPDATA = Join-Path $projectRoot '.cache\appdata'
$env:NUGET_PACKAGES = Join-Path $projectRoot '.cache\packages'
New-Item -ItemType Directory -Path $env:APPDATA -Force | Out-Null

function Restore-OC2BuildEnvironment {
    foreach ($name in $oc2SavedEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $oc2SavedEnvironment[$name], 'Process')
    }
}

function Get-OC2GameRoot {
    param([string]$RequestedPath)
    if ($RequestedPath) { return (Resolve-Path -LiteralPath $RequestedPath).Path }
    if ($env:OC2_GAME_DIR) { return (Resolve-Path -LiteralPath $env:OC2_GAME_DIR).Path }
    $steamRoots = @()
    foreach ($key in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
        if (Test-Path -LiteralPath $key) {
            $item = Get-ItemProperty -LiteralPath $key
            if ($item.SteamPath) { $steamRoots += $item.SteamPath }
            if ($item.InstallPath) { $steamRoots += $item.InstallPath }
        }
    }
    $libraries = @($steamRoots)
    foreach ($steamRoot in $steamRoots) {
        $file = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
        if (Test-Path -LiteralPath $file) {
            foreach ($match in [regex]::Matches((Get-Content -LiteralPath $file -Raw), '"path"\s+"([^"]+)"')) {
                $libraries += $match.Groups[1].Value.Replace('\\', '\')
            }
        }
    }
    foreach ($library in $libraries | Select-Object -Unique) {
        $candidate = Join-Path $library 'steamapps\common\Overcooked! 2'
        if (Test-Path -LiteralPath (Join-Path $candidate 'Overcooked2_Data\Managed\Assembly-CSharp.dll')) { return $candidate }
    }
    throw 'Game not found. Pass -GameRoot or set OC2_GAME_DIR to your local game directory.'
}
