[CmdletBinding()]
param(
    [switch]$Web
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $projectRoot
if ($Web) {
    Set-Location -LiteralPath (Join-Path $projectRoot 'ACTIVE_frontend_lalamog')
    $npm = if ($env:OS -eq 'Windows_NT') { 'npm.cmd' } else { 'npm' }
    if (!(Test-Path -LiteralPath 'node_modules')) {
        & $npm ci
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    & $npm run dev
    exit $LASTEXITCODE
}
Set-Location -LiteralPath (Join-Path $projectRoot 'ACTIVE_backend_lalamog')
dotnet run --project Lalamog.Api.csproj
exit $LASTEXITCODE
