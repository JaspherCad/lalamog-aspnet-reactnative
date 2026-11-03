<# Validate shared knowledge entry files, local link targets and feature routing. #>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$knowledgeRoot = Join-Path $projectRoot '_DOCUMENTATION'
$entryPaths = @(
    'AGENTS.md'
    'GEMINI.md'
    'ACTIVE_backend_lalamog/AGENTS.md'
    'ACTIVE_frontend_lalamog/AGENTS.md'
    'tests/AGENTS.md'
)
$requiredKnowledge = @(
    'README.md', 'CODEX_BOOTSTRAP.md', 'ENGINEERING.md', 'GETTING_STARTED.md',
    'REPOSITORY_MAP.md', 'PROJECT_STATE.md', 'ARCHITECTURE.md', 'DOMAIN_RULES.md',
    'API_CONTRACTS.md', 'DATA_MODEL.md', 'DESIGN.md', 'DESIGN_BRIEF.md', 'BACKLOG.md',
    'features/README.md', 'features/AGENT_WORKFLOW.md', 'templates/FEATURE.md',
    'scopes/BACKEND.md', 'scopes/FRONTEND.md', 'scopes/TESTS.md',
    'scopes/INFRASTRUCTURE.md', 'scopes/TOOLING.md', 'scopes/LEGACY.md',
    'runbooks/DEVELOPMENT.md', 'runbooks/TESTING.md', 'runbooks/RECOVERY.md',
    'memory/PROTOCOL.md', 'memory/HANDOFF.md', 'memory/CHANGELOG.md', 'decisions/README.md'
)
foreach ($relativePath in $entryPaths) {
    if (!(Test-Path -LiteralPath (Join-Path $projectRoot $relativePath) -PathType Leaf)) {
        throw "Missing entry file: $relativePath"
    }
}
foreach ($relativePath in $requiredKnowledge) {
    if (!(Test-Path -LiteralPath (Join-Path $knowledgeRoot $relativePath) -PathType Leaf)) {
        throw "Missing knowledge file: _DOCUMENTATION/$relativePath"
    }
}
$knowledgeFiles = @($entryPaths | ForEach-Object { Get-Item -LiteralPath (Join-Path $projectRoot $_) })
$knowledgeFiles += @(Get-ChildItem -LiteralPath $knowledgeRoot -Filter '*.md' -Recurse -File)
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
$featureIndex = Get-Content -LiteralPath (Join-Path $knowledgeRoot 'features/README.md') -Raw -Encoding UTF8
$bootstrap = Get-Content -LiteralPath (Join-Path $knowledgeRoot 'CODEX_BOOTSTRAP.md') -Raw -Encoding UTF8
$features = @(Get-ChildItem -LiteralPath (Join-Path $knowledgeRoot 'features') -Filter '*.md' -File | Where-Object Name -ne 'README.md')
foreach ($guide in $features) {
    $escapedName = [regex]::Escape($guide.Name)
    if ($featureIndex -notmatch ('\]\(' + $escapedName + '(?:#[^)]*)?\)')) {
        $failures += ('Unregistered feature in features/README.md: ' + $guide.Name)
    }
    if ($bootstrap -notmatch ('\]\(features/' + $escapedName + '(?:#[^)]*)?\)')) {
        $failures += ('Unregistered feature in CODEX_BOOTSTRAP.md: ' + $guide.Name)
    }
}
if ($failures.Count -gt 0) { throw ($failures -join [Environment]::NewLine) }
Write-Output ("Knowledge OK: {0} Markdown files, {1} local targets, {2} feature guides registered in both routers. External URLs and heading fragments are not validated." -f $knowledgeFiles.Count, $linkCount, $features.Count)
