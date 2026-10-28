param([switch]$Initialize)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$envPath = Join-Path $projectRoot '.env'
if (!(Test-Path -LiteralPath $envPath)) { throw 'Run ./scripts/setup.ps1 first.' }
$settings = @{}
Get-Content -LiteralPath $envPath | ForEach-Object {
    if ($_ -match '^([A-Z_]+)=(.*)$') { $settings[$matches[1]] = $matches[2] }
}
$env:ConnectionStrings__Default = "Server=localhost,14333;Database=Lalamog;User Id=sa;Password=$($settings.MSSQL_SA_PASSWORD);Encrypt=True;TrustServerCertificate=True"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Storage__Path = Join-Path $projectRoot '.cache/uploads'
$env:DataProtection__Path = Join-Path $projectRoot '.cache/keys'
$env:Seed__Demo = $settings.SEED_DEMO
$env:Seed__Password = $settings.DEMO_PASSWORD
Set-Location -LiteralPath $projectRoot
if ($Initialize) {
    dotnet run --project src/Lalamog.Api --no-launch-profile -- --initialize
} else {
    dotnet run --project src/Lalamog.Api --no-launch-profile --urls http://localhost:5080
}
exit $LASTEXITCODE