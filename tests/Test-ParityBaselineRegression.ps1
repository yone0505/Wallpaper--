$ErrorActionPreference = 'Stop'
$taskRepositoryRoot = Split-Path -Parent $PSScriptRoot
$taskBaselineText = Get-Content -LiteralPath (Join-Path $taskRepositoryRoot 'docs\reference\parity-baseline.json') -Raw -Encoding UTF8
$taskFixtureDirectory = Join-Path $taskRepositoryRoot 'work\test-fixtures'
New-Item -ItemType Directory -Path $taskFixtureDirectory -Force | Out-Null
$taskCases = @('premature-completion', 'verified-without-evidence', 'missing-editor-scope')
foreach ($taskCase in $taskCases) {
    $taskFixture = $taskBaselineText | ConvertFrom-Json
    switch ($taskCase) {
        'premature-completion' { $taskFixture.completion.fullParityClaimAllowed = $true }
        'verified-without-evidence' { $taskFixture.features[0].status = 'verified' }
        'missing-editor-scope' { $taskFixture.features = @($taskFixture.features | Where-Object { $_.category -ne 'editor' }) }
    }
    $taskFixturePath = Join-Path $taskFixtureDirectory "$taskCase-$([Guid]::NewGuid().ToString('N')).json"
    $taskFixture | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $taskFixturePath -Encoding UTF8
    try {
        $taskRejected = $false
        try { & (Join-Path $PSScriptRoot 'Test-ParityBaseline.ps1') -BaselinePath $taskFixturePath | Out-Null }
        catch { $taskRejected = $true }
        if (-not $taskRejected) { throw "错误的验收记录未被拒绝：$taskCase" }
        Write-Output "PASS: rejected $taskCase"
    } finally {
        Remove-Item -LiteralPath $taskFixturePath
    }
}
