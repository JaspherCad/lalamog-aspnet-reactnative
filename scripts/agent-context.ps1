<#
.SYNOPSIS
Print a provider-neutral Markdown context packet using an explicit safe file list.
.EXAMPLE
./scripts/agent-context.ps1 -Feature maps,media
#>
[CmdletBinding()]
param(
    [ValidateSet('auth', 'profiles', 'matching', 'chat', 'sessions', 'media', 'maps', 'background', 'workflow')]
    [string[]]$Feature = @(),
    # Newly registered guides need not wait for a built-in alias.
    [string[]]$FeatureFile = @()
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$featureFiles = @{
    auth = 'docs/features/AUTHENTICATION.md'
    profiles = 'docs/features/PROFILES_AND_DISCOVERY.md'
    matching = 'docs/features/MATCHING.md'
    chat = 'docs/features/CHAT.md'
    sessions = 'docs/features/SESSIONS_AND_WAIVERS.md'
    media = 'docs/features/MEDIA_STORAGE.md'
    maps = 'docs/features/MAPS_AND_AREAS.md'
    background = 'docs/features/BACKGROUND_WORKFLOWS.md'
    workflow = 'docs/features/AGENT_WORKFLOW.md'
}
$contextFiles = @(
    'AGENTS.md'
    'codex-bootstrap.md'
    'Instructions.md'
    'docs/PROJECT_STATE.md'
    'docs/README.md'
    'docs/ARCHITECTURE.md'
    'docs/API_CONTRACTS.md'
    'docs/DATA_MODEL.md'
    'docs/features/README.md'
    'docs/decisions/README.md'
    'docs/memory/HANDOFF.md'
    'docs/memory/PROTOCOL.md'
    'backlog-rewrite.md'
    'docs/backlogs/FRONTEND.md'
    'docs/backlogs/BACKEND.md'
)
foreach ($featureName in $Feature) { $contextFiles += $featureFiles[$featureName] }
$featuresRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'docs/features'))
foreach ($featurePath in $FeatureFile) {
    if ([IO.Path]::IsPathRooted($featurePath)) { throw 'FeatureFile must be a repository-relative docs/features/*.md path.' }
    $featureFullPath = [IO.Path]::GetFullPath((Join-Path $projectRoot $featurePath))
    if (![string]::Equals([IO.Path]::GetDirectoryName($featureFullPath), $featuresRoot, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetExtension($featureFullPath) -ine '.md' -or
        [IO.Path]::GetFileName($featureFullPath) -ieq 'README.md') {
        throw 'FeatureFile must name a feature Markdown directly inside docs/features/.'
    }
    $featureItem = Get-Item -LiteralPath $featureFullPath
    if ($featureItem.PSIsContainer -or ($featureItem.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw 'FeatureFile must be an ordinary Markdown file, not a directory or link.'
    }
    $contextFiles += 'docs/features/' + $featureItem.Name
}
$contextFiles = @($contextFiles | Select-Object -Unique)
# Validate the entire manifest before emitting a partial packet.
foreach ($relativePath in $contextFiles) {
    if (!(Test-Path -LiteralPath (Join-Path $projectRoot $relativePath) -PathType Leaf)) {
        throw "Missing knowledge file: $relativePath"
    }
}
Write-Output '# LALAMOG project context packet'
Write-Output ''
Write-Output ('Generated UTC: ' + [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ'))
Write-Output 'Markdown baseline only. Inspect current source before editing; no source files, secrets, runtime data, or private uploads are included.'
Write-Output 'Relative links inside each section resolve from that original file path in the repository.'
foreach ($relativePath in $contextFiles) {
    Write-Output ''
    Write-Output '---'
    Write-Output ('# File: ' + $relativePath)
    Write-Output ''
    Write-Output (Get-Content -LiteralPath (Join-Path $projectRoot $relativePath) -Raw -Encoding UTF8)
}
