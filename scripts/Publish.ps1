param([string]$OutputDirectory = '', [string]$DotNet = '')
$ErrorActionPreference = 'Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
if (-not $OutputDirectory) { $OutputDirectory=Join-Path $projectRoot 'outputs\WallpaperDesktop' }
if (-not $DotNet) {
    $found=Get-Command dotnet -ErrorAction SilentlyContinue
    $DotNet=if ($found) {$found.Source} else {Join-Path $env:LOCALAPPDATA 'Programs\REA\engines\dotnet\dotnet.exe'}
    if (-not (& $DotNet --list-sdks)) { $DotNet=Join-Path $env:LOCALAPPDATA 'Programs\REA\engines\dotnet\dotnet.exe' }
}
& $DotNet publish (Join-Path $projectRoot 'src\Wallpaper.App') -c Release -r win-x64 --self-contained true -o $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
Write-Output (Join-Path $OutputDirectory 'WallpaperDesktop.exe')
