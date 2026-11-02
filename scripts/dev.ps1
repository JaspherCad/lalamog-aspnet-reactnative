[CmdletBinding(DefaultParameterSetName = 'Api')]
param(
    [Parameter(ParameterSetName = 'Api')][switch]$Initialize,
    [Parameter(Mandatory, ParameterSetName = 'Web')][switch]$Web
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
$envPath = Join-Path $projectRoot 'infrastructure/.env'
if (!(Test-Path -LiteralPath $envPath)) { throw 'Run ./scripts/setup.ps1 first.' }
$settings = @{}
Get-Content -LiteralPath $envPath | ForEach-Object {
    if ($_ -match '^([A-Z_]+)=(.*)$') { $settings[$matches[1]] = $matches[2] }
}
$env:ConnectionStrings__Default = "Server=tcp:127.0.0.1,14333;Database=Lalamog;User Id=sa;Password=$($settings.MSSQL_SA_PASSWORD);Encrypt=True;TrustServerCertificate=True"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Storage__Path = Join-Path $projectRoot '.cache/uploads'
$env:DataProtection__Path = Join-Path $projectRoot '.cache/keys'
$env:Seed__Demo = if ($settings.ContainsKey('SEED_DEMO')) { $settings.SEED_DEMO } else { 'true' }
$env:Seed__Password = $settings.DEMO_PASSWORD
if ([string]::IsNullOrWhiteSpace($settings.MSSQL_SA_PASSWORD)) { throw 'Missing MSSQL_SA_PASSWORD. Run ./scripts/setup.ps1.' }
if ($Initialize -and $env:Seed__Demo -eq 'true' -and [string]::IsNullOrWhiteSpace($settings.DEMO_PASSWORD)) {
    throw 'Missing DEMO_PASSWORD. Run ./scripts/setup.ps1, or set SEED_DEMO=false.'
}
Set-Location -LiteralPath (Join-Path $projectRoot 'ACTIVE_backend_lalamog')
if ($Initialize) {
    dotnet run --project Lalamog.Api.csproj --no-launch-profile -- --initialize
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
dotnet run --project Lalamog.Api.csproj --no-launch-profile --urls http://localhost:5080
exit $LASTEXITCODE
