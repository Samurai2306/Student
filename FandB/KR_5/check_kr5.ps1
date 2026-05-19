Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Join-Path $PSScriptRoot "social-feed"

Write-Host "== KR5: docker compose ==" -ForegroundColor Cyan
Push-Location $root
docker compose up -d --build | Out-Null
Pop-Location
Start-Sleep -Seconds 5

Write-Host "API health:" -ForegroundColor Yellow
(Invoke-RestMethod "http://localhost:4000/health") | Format-List

Write-Host "Frontend:" -ForegroundColor Yellow
try {
  $r = Invoke-WebRequest "http://localhost:3000/" -UseBasicParsing -TimeoutSec 5
  Write-Host ("  HTTP " + $r.StatusCode)
} catch {
  Write-Host "  FAILED"
}

Write-Host "Backend tests:" -ForegroundColor Yellow
Push-Location (Join-Path $root "backend")
npm test 2>&1 | Select-Object -Last 8
Pop-Location

Write-Host "Done. Open http://localhost:3000" -ForegroundColor Green
