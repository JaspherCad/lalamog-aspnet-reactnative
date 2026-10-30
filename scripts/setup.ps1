$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$infrastructure = Join-Path $projectRoot 'infrastructure'
New-Item -ItemType Directory -Path $infrastructure -Force | Out-Null
if (Test-Path -LiteralPath (Join-Path $projectRoot '.git')) {
    git -C $projectRoot config --local core.excludesFile (Join-Path $PSScriptRoot 'gitignore')
    if ($LASTEXITCODE -ne 0) { throw 'Unable to configure repository ignore rules.' }
}
$envPath = Join-Path $infrastructure '.env'
function New-Secret {
    $bytes = New-Object byte[] 24
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return [Convert]::ToBase64String($bytes)
}
$defaults = [ordered]@{
    MSSQL_SA_PASSWORD = "Aa1!$(New-Secret)"
    RABBITMQ_USER = 'lalamog'
    RABBITMQ_PASSWORD = New-Secret
    SEED_DEMO = 'true'
    DEMO_PASSWORD = "Aa1!$(New-Secret)"
}
$existing = @()
if (Test-Path -LiteralPath $envPath) { $existing = @(Get-Content -LiteralPath $envPath) }
$names = @($existing | ForEach-Object { if ($_ -match '^([A-Z_]+)=') { $matches[1] } })
$missing = @($defaults.Keys | Where-Object { $_ -notin $names } | ForEach-Object { "$_=$($defaults[$_])" })
if ($missing.Count -gt 0) {
    if ($existing.Count -gt 0) { $existing += '' }
    [IO.File]::WriteAllLines($envPath, @($existing) + $missing)
}
Write-Host 'infrastructure/.env is ready; existing credentials were preserved.'
Write-Host '1. Supporting services: ./scripts/services.ps1'
Write-Host '2. API (first run): ./scripts/dev.ps1 -Initialize'
Write-Host '   API (later runs): ./scripts/dev.ps1'
Write-Host '3. Frontend (another terminal): ./scripts/dev.ps1 -Web'
Write-Host 'Open http://localhost:5173. The API runs at http://localhost:5080.'
Write-Host 'Demo emails: alex@example.test, sam@example.test, jordan@example.test. Password: DEMO_PASSWORD in infrastructure/.env.'
