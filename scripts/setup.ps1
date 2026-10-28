$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$envPath = Join-Path $projectRoot '.env'
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
    WEB_PORT = '8080'
}
$existing = @()
if (Test-Path -LiteralPath $envPath) { $existing = @(Get-Content -LiteralPath $envPath) }
$names = @($existing | ForEach-Object { if ($_ -match '^([A-Z_]+)=') { $matches[1] } })
$missing = @($defaults.Keys | Where-Object { $_ -notin $names } | ForEach-Object { "$_=$($defaults[$_])" })
if ($missing.Count -gt 0) {
    if ($existing.Count -gt 0) { $existing += '' }
    [IO.File]::WriteAllLines($envPath, @($existing) + $missing)
}
Write-Host '.env is ready; existing credentials were preserved.'
Write-Host 'Start Docker Desktop, then run docker compose up --build -d.'
Write-Host 'Demo emails: alex@example.test, sam@example.test, jordan@example.test. Password: DEMO_PASSWORD in .env.'