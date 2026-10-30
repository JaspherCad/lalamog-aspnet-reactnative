# Compose lives with its private environment file; the repository root stays folder-only.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$infrastructure = Join-Path $projectRoot 'infrastructure'
$composeArgs = @($args)
if ($composeArgs.Count -eq 0) { $composeArgs = @('up', '-d', '--wait') }
& docker compose --project-directory $infrastructure --file (Join-Path $infrastructure 'compose.yaml') @composeArgs
exit $LASTEXITCODE
