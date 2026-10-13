param([string]$DotNet = '')
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if (-not $DotNet) {
    $found = Get-Command dotnet -ErrorAction SilentlyContinue
    $DotNet = if ($found) { $found.Source } else { Join-Path $env:LOCALAPPDATA 'Programs\REA\engines\dotnet\dotnet.exe' }
    $sdks = & $DotNet --list-sdks
    if (-not $sdks) { $DotNet = Join-Path $env:LOCALAPPDATA 'Programs\REA\engines\dotnet\dotnet.exe' }
}
& ./tests/Test-RepositoryPolicy.ps1
& ./tests/Test-IgnoredArtifacts.ps1
& ./tests/Test-ParityBaseline.ps1
& ./tests/Test-ParityBaselineRegression.ps1
& $DotNet run --project tests/Wallpaper.Core.Tests -c Release
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
& node --test tests/ui-model.test.mjs
if ($LASTEXITCODE -ne 0) { throw 'UI model tests failed' }
if (Test-Path ./src/Wallpaper.App/Wallpaper.App.csproj) {
    & $DotNet build src/Wallpaper.App -c Release
    if ($LASTEXITCODE -ne 0) { throw 'App build failed' }
}
& git diff --check
if ($LASTEXITCODE -ne 0) { throw 'Whitespace validation failed' }
Write-Output 'PASS: all automated checks'
