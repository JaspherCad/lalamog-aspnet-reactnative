<#
.SYNOPSIS
Print a provider-neutral Markdown packet from an explicit safe manifest.
.EXAMPLE
./scripts/agent-context.ps1 -Feature maps,media
.EXAMPLE
./scripts/agent-context.ps1 -FeatureFile _DOCUMENTATION/features/AGENT_WORKFLOW.md
#>
[CmdletBinding()]
param(
    [ValidateSet('auth', 'profiles', 'matching', 'chat', 'sessions', 'media', 'maps', 'background', 'workflow', 'design', 'backend', 'local')]
    [string[]]$Feature = @(),
    [string[]]$FeatureFile = @()
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$featureFiles = @{
    auth = '_DOCUMENTATION/features/AUTHENTICATION.md'
    profiles = '_DOCUMENTATION/features/PROFILES_AND_DISCOVERY.md'
    matching = '_DOCUMENTATION/features/MATCHING.md'
    chat = '_DOCUMENTATION/features/CHAT.md'
    sessions = '_DOCUMENTATION/features/SESSIONS_AND_WAIVERS.md'
    media = '_DOCUMENTATION/features/MEDIA_STORAGE.md'
    maps = '_DOCUMENTATION/features/MAPS_AND_AREAS.md'
    background = '_DOCUMENTATION/features/BACKGROUND_WORKFLOWS.md'
    workflow = '_DOCUMENTATION/features/AGENT_WORKFLOW.md'
    design = '_DOCUMENTATION/features/FRONTEND_DESIGN.md'
    backend = '_DOCUMENTATION/features/BACKEND_STRUCTURE.md'
    local = '_DOCUMENTATION/features/LOCAL_DEVELOPMENT.md'
}
$contextFiles = @(
    'AGENTS.md'
    '_DOCUMENTATION/CODEX_BOOTSTRAP.md'
    '_DOCUMENTATION/ENGINEERING.md'
    '_DOCUMENTATION/PROJECT_STATE.md'
    '_DOCUMENTATION/README.md'
    '_DOCUMENTATION/REPOSITORY_MAP.md'
    '_DOCUMENTATION/ARCHITECTURE.md'
    '_DOCUMENTATION/DOMAIN_RULES.md'
    '_DOCUMENTATION/API_CONTRACTS.md'
    '_DOCUMENTATION/DATA_MODEL.md'
    '_DOCUMENTATION/features/README.md'
    '_DOCUMENTATION/decisions/README.md'
    '_DOCUMENTATION/memory/HANDOFF.md'
    '_DOCUMENTATION/memory/PROTOCOL.md'
)
foreach ($featureName in $Feature) { $contextFiles += $featureFiles[$featureName] }
$featuresRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot '_DOCUMENTATION/features'))
foreach ($featurePath in $FeatureFile) {
    if ([IO.Path]::IsPathRooted($featurePath)) {
        throw 'FeatureFile must be a repository-relative _DOCUMENTATION/features/*.md path.'
    }
    $featureFullPath = [IO.Path]::GetFullPath((Join-Path $projectRoot $featurePath))
    if (![string]::Equals([IO.Path]::GetDirectoryName($featureFullPath), $featuresRoot, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetExtension($featureFullPath) -ine '.md' -or
        [IO.Path]::GetFileName($featureFullPath) -ieq 'README.md') {
        throw 'FeatureFile must name a guide directly inside _DOCUMENTATION/features/, excluding README.md.'
    }
    $contextFiles += '_DOCUMENTATION/features/' + [IO.Path]::GetFileName($featureFullPath)
}
# Validate every path and ancestor before any packet output. Never follow linked
# Markdown or linked feature directories into private settings or runtime data.
function Assert-OrdinaryKnowledgeFile([string]$relativePath) {
    $fullPath = [IO.Path]::GetFullPath((Join-Path $projectRoot $relativePath))
    if (!$fullPath.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Knowledge file must remain inside the repository.'
    }
    $item = Get-Item -LiteralPath $fullPath -Force
    if ($item.PSIsContainer -or $item.Extension -ine '.md') { throw 'Knowledge entry must be a Markdown file.' }
    $currentPath = $fullPath
    while ($true) {
        $currentItem = Get-Item -LiteralPath $currentPath -Force
        if ($currentItem.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw 'Knowledge files and directories must not be links or reparse points.'
        }
        if ([string]::Equals($currentPath, $projectRoot, [StringComparison]::OrdinalIgnoreCase)) { break }
        $currentPath = [IO.Path]::GetDirectoryName($currentPath)
    }
}
$uniquePaths = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
$contextFiles = @($contextFiles | Where-Object { $uniquePaths.Add($_) })
foreach ($relativePath in $contextFiles) { Assert-OrdinaryKnowledgeFile $relativePath }
Write-Output '# LALAMOG project context packet'
Write-Output ''
Write-Output ('Generated UTC: ' + [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ'))
Write-Output 'Markdown baseline only. Inspect current source before editing; no source files, secrets, runtime data, private uploads or archives are included.'
Write-Output 'File labels are relative to the repository root; Markdown links resolve from each original document.'
foreach ($relativePath in $contextFiles) {
    Write-Output ''
    Write-Output '---'
    Write-Output ('# File: ' + $relativePath)
    Write-Output ''
    Write-Output (Get-Content -LiteralPath (Join-Path $projectRoot $relativePath) -Raw -Encoding UTF8)
}
