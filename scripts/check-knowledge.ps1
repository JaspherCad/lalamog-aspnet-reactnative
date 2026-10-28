<# Check local Markdown link targets in current project knowledge. Does not execute links. #>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$rootNames = @('AGENTS.md', 'web/AGENTS.md', 'src/Lalamog.Api/AGENTS.md', 'tests/AGENTS.md', 'Instructions.md', 'codex-bootstrap.md', 'GEMINI.md', 'README.md', 'backlog-rewrite.md', 'FIGHT_RESULTS_GUIDE.md')
$knowledgeFiles = @($rootNames | ForEach-Object { Get-Item -LiteralPath (Join-Path $projectRoot $_) })
$knowledgeFiles += @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'docs') -Filter '*.md' -Recurse -File)
$failures = @()
$linkCount = 0
foreach ($knowledgeFile in $knowledgeFiles) {
    $body = Get-Content -LiteralPath $knowledgeFile.FullName -Raw -Encoding UTF8
    foreach ($link in [regex]::Matches([string]$body, '!?\[[^\]]*\]\(([^)]+)\)')) {
        $target = $link.Groups[1].Value.Trim().Trim('<', '>')
        if ($target -match '^[a-zA-Z][a-zA-Z0-9+.-]*:' -or $target.StartsWith('#')) { continue }
        $target = [Uri]::UnescapeDataString(($target -split '#', 2)[0])
        if ([string]::IsNullOrWhiteSpace($target)) { continue }
        $linkCount++
        $resolved = Join-Path $knowledgeFile.DirectoryName $target
        if (!(Test-Path -LiteralPath $resolved)) {
            $failures += ($knowledgeFile.FullName + ': missing ' + $target)
        }
    }
}
if ($failures.Count -gt 0) { throw ($failures -join [Environment]::NewLine) }
Write-Output ("Knowledge links OK: {0} Markdown files, {1} local targets. External URLs and fragments are not validated." -f $knowledgeFiles.Count, $linkCount)
