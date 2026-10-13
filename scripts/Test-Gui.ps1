param([string]$ExePath = '')
$ErrorActionPreference = 'Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Set-Location $projectRoot
if (-not $ExePath) {
    $release=Join-Path $projectRoot 'work\gui-build'
    & ./scripts/Publish.ps1 -OutputDirectory $release
    $ExePath=Join-Path $release 'WallpaperDesktop.exe'
}
$ExePath=(Resolve-Path -LiteralPath $ExePath).Path
if (-not (Test-Path 'work/qa/node_modules/playwright/package.json')) {
    & npm install --prefix work/qa playwright@1.64.0 --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'Playwright setup failed' }
}
$profile=Join-Path $projectRoot ('work\gui-profile-'+[guid]::NewGuid().ToString('N'))
$fixture=Join-Path $projectRoot 'tests\fixtures\orbit.webm'
$process=Start-Process -FilePath $ExePath -ArgumentList @('--data-dir',('"'+$profile+'"'),'--debug-port','32222','--import',('"'+$fixture+'"')) -WindowStyle Hidden -PassThru
$env:WALLPAPER_EXE=$ExePath
try {
    $ready=$false
    for ($attempt=0; $attempt -lt 80; $attempt++) {
        if ($process.HasExited) { throw 'GUI application exited before initialization' }
        try {
            $targets=Invoke-RestMethod 'http://127.0.0.1:32222/json/list' -TimeoutSec 1
            if (@($targets | Where-Object {$_.url -eq 'https://manager.wallpaper.local/index.html'}).Count -gt 0) { $ready=$true; break }
        } catch { }
        Start-Sleep -Milliseconds 100
    }
    if (-not $ready) { throw 'GUI debug endpoint did not start' }
    & node tests/gui-smoke.mjs
    if ($LASTEXITCODE -ne 0) { throw 'GUI smoke failed' }
    & node tests/gui-restart.mjs
    if ($LASTEXITCODE -ne 0) { throw 'GUI restart failed' }
    & node tests/workshop-gui.mjs
    if ($LASTEXITCODE -ne 0) { throw 'Workshop GUI tests failed' }
    Write-Output 'PASS: all native GUI checks'
} finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id }
    Remove-Item Env:WALLPAPER_EXE -ErrorAction SilentlyContinue
}
