$ErrorActionPreference = 'Stop'
$taskRepositoryRoot = Split-Path -Parent $PSScriptRoot
$taskInstructionsPath = Join-Path $taskRepositoryRoot 'AGENTS.md'
$taskUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
$taskInstructions = [System.IO.File]::ReadAllText($taskInstructionsPath, $taskUtf8)

$taskRequirements = [ordered]@{
    '要求每次修改更新测试' = '每次改动后，都必须编写或更新相关测试'
    '要求交付前通过全部验证' = '交付前，确保所有测试和验证全部通过'
    '记录指定 GitHub 仓库' = 'https://github.com/yone0505/Wallpaper--'
    '要求提交并推送修改' = '提交 commit 并推送'
}

$taskFailures = [System.Collections.Generic.List[string]]::new()
foreach ($taskRequirement in $taskRequirements.GetEnumerator()) {
    if ($taskInstructions.Contains($taskRequirement.Value)) {
        Write-Output "PASS: $($taskRequirement.Key)"
    } else {
        $taskFailures.Add($taskRequirement.Key)
        Write-Output "FAIL: $($taskRequirement.Key)"
    }
}

if ($taskFailures.Count -gt 0) {
    throw "项目规范校验失败：$($taskFailures -join '、')"
}
Write-Output "全部 $($taskRequirements.Count) 项项目规范校验通过。"
