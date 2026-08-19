# Refresh official Stedi OpenAPI specs and regenerate models.
# Usage: ./scripts/update-openapi.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$openapi = Join-Path $root "openapi"
New-Item -ItemType Directory -Force -Path $openapi | Out-Null

$files = @(
    "healthcare.json",
    "claims.json",
    "enrollment.json",
    "payers.json",
    "core.json",
    "manager.json",
    "event-destinations.json"
)

foreach ($file in $files) {
    $url = "https://raw.githubusercontent.com/Stedi/openApi/main/$file"
    $dest = Join-Path $openapi $file
    Write-Host "Downloading $url"
    Invoke-WebRequest -Uri $url -OutFile $dest -UseBasicParsing
}

Push-Location $root
try {
    node .\scripts\generate-models.js
}
finally {
    Pop-Location
}

Write-Host "OpenAPI specs and generated models updated. Review git diff before committing."
