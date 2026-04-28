Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

function Invoke-HttpStatus([string]$Url) {
  try {
    $resp = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 5
    return [int]$resp.StatusCode
  } catch {
    return 0
  }
}

function Start-NodeService([string]$WorkingDir, [string]$Script, [int]$WaitSeconds = 2) {
  $wd = Resolve-Path (Join-Path $repoRoot $WorkingDir)
  $p = Start-Process -FilePath "node" -WorkingDirectory $wd -ArgumentList $Script -PassThru -WindowStyle Hidden
  Start-Sleep -Seconds $WaitSeconds
  return $p
}

function Stop-Proc([System.Diagnostics.Process]$Proc) {
  if ($null -eq $Proc) { return }
  try { Stop-Process -Id $Proc.Id -Force -ErrorAction SilentlyContinue } catch {}
}

Write-Host "== Practice 19 (Postgres) ==" -ForegroundColor Cyan
Push-Location (Join-Path $repoRoot "KR_4/practice_19_postgres_api")
docker compose up -d | Out-Null
Pop-Location

$p19 = $null
try {
  $p19 = Start-NodeService "KR_4/practice_19_postgres_api" "server.js"
  Write-Host ("health: " + (Invoke-HttpStatus "http://localhost:3005/health"))
  Write-Host ("docs:   " + (Invoke-HttpStatus "http://localhost:3005/docs"))
} finally {
  Stop-Proc $p19
}

Write-Host "== Practice 20 (Mongo) ==" -ForegroundColor Cyan
Push-Location (Join-Path $repoRoot "KR_4/practice_20_mongo_api")
docker compose up -d | Out-Null
Pop-Location

$p20 = $null
try {
  $p20 = Start-NodeService "KR_4/practice_20_mongo_api" "server.js"
  Write-Host ("health: " + (Invoke-HttpStatus "http://localhost:3006/health"))
  Write-Host ("docs:   " + (Invoke-HttpStatus "http://localhost:3006/docs"))
} finally {
  Stop-Proc $p20
}

Write-Host "== Practice 21 (Redis cache + Practice 11 server) ==" -ForegroundColor Cyan
docker rm -f redis-cache 2>$null | Out-Null
docker run -d --name redis-cache -p 6379:6379 redis | Out-Null

$p11 = $null
try {
  $p11 = Start-NodeService "KR_2/Practice_11/server" "app.js"
  Write-Host ("health: " + (Invoke-HttpStatus "http://localhost:3000/health"))
  Write-Host "Cache header check requires auth; use client or Postman for /api/* endpoints."
} finally {
  Stop-Proc $p11
}

Write-Host "== Practice 23 (Docker Compose: Nginx + 2 backends) ==" -ForegroundColor Cyan
Push-Location (Join-Path $repoRoot "KR_4/practice_23_docker_compose")
docker compose up -d --build | Out-Null
Pop-Location
Write-Host ("lb:     " + (Invoke-HttpStatus "http://localhost:8088/"))

Write-Host "Done." -ForegroundColor Green

