param([string]$BaselinePath = '')
$ErrorActionPreference = 'Stop'
$taskRepositoryRoot = Split-Path -Parent $PSScriptRoot
$taskBaselinePath = if ([string]::IsNullOrWhiteSpace($BaselinePath)) { Join-Path $taskRepositoryRoot 'docs\reference\parity-baseline.json' } else { $BaselinePath }
$taskBaseline = Get-Content -LiteralPath $taskBaselinePath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($taskBaseline.schemaVersion -ne 1 -or $taskBaseline.target.version -ne '2.8.42' -or $taskBaseline.target.fileVersion -ne '2.8.0.42') {
    throw '原版版本基线不完整或不一致。'
}
$taskExpectedCategories = @('interface','library','displays','rendering','playback','audio','playlists','system','editor','steam','mobile','rgb')
$taskIds = [System.Collections.Generic.HashSet[string]]::new()
$taskCategories = [System.Collections.Generic.HashSet[string]]::new()
$taskAllVerified = $true
foreach ($taskFeature in $taskBaseline.features) {
    if (-not $taskIds.Add($taskFeature.id)) { throw "重复验收项目：$($taskFeature.id)" }
    if (-not $taskExpectedCategories.Contains($taskFeature.category)) { throw "未知验收类别：$($taskFeature.category)" }
    $taskCategories.Add($taskFeature.category) | Out-Null
    if ([string]::IsNullOrWhiteSpace($taskFeature.acceptance)) { throw "验收条件为空：$($taskFeature.id)" }
    if ($taskFeature.status -notin @('unverified','implemented','verified','blocked')) { throw "未知验收状态：$($taskFeature.id)" }
    if ($taskFeature.verification.Count -eq 0) { throw "缺少验证方式：$($taskFeature.id)" }
    if ($taskFeature.status -eq 'verified') {
        foreach ($taskKind in $taskFeature.verification) {
            if ($taskFeature.evidence.$taskKind.Count -eq 0) { throw "声称已通过却缺少证据：$($taskFeature.id) / $taskKind" }
        }
    } else { $taskAllVerified = $false }
}
foreach ($taskCategory in $taskExpectedCategories) {
    if (-not $taskCategories.Contains($taskCategory)) { throw "缺少功能类别：$taskCategory" }
}
if ($taskBaseline.features.Count -lt 50) { throw '完整软件的验收范围被意外缩减。' }
if ($taskBaseline.completion.fullParityClaimAllowed -ne $taskAllVerified) {
    throw '完整一致的声明与逐项验收状态不符。'
}
Write-Output "PASS: 固定版本与 $($taskCategories.Count) 个功能类别。"
Write-Output "PASS: $($taskIds.Count) 个唯一验收项目都有条件与验证方式。"
Write-Output 'PASS: 未通过的项目禁止被报告为完整一致。'
Write-Output '本测试校验验收记录结构；实际功能是否一致以各项目的运行与视觉证据为准。'
