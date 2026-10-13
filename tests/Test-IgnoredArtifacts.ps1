$ErrorActionPreference = 'Stop'
$taskRepositoryRoot = Split-Path -Parent $PSScriptRoot
$taskGit = (Get-Command git -ErrorAction Stop).Source
Push-Location -LiteralPath $taskRepositoryRoot
try {
    foreach ($taskPath in @('work/reference/ui.png', 'outputs/WallpaperClone.exe', 'src/Wallpaper.App/bin/Debug/app.exe', 'src/Wallpaper.App/obj/project.assets.json', 'node_modules/example/index.js', '.env')) {
        & $taskGit check-ignore --quiet --no-index -- $taskPath
        if ($LASTEXITCODE -ne 0) { throw "临时或构建文件未被忽略：$taskPath" }
        Write-Output "PASS: ignored $taskPath"
    }
    foreach ($taskPath in @('src/Wallpaper.App/Program.cs', 'tests/Test-RepositoryPolicy.ps1', 'docs/reference/baseline.md')) {
        & $taskGit check-ignore --quiet --no-index -- $taskPath
        if ($LASTEXITCODE -ne 1) { throw "源码、测试或设计文档被意外忽略：$taskPath" }
        Write-Output "PASS: tracked $taskPath"
    }
} finally {
    Pop-Location
}
