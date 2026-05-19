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

function Start-NodeService([string]$WorkingDir, [string]$Script, [hashtable]$Env = @{}, [int]$WaitSeconds = 3) {
  $wd = (Resolve-Path (Join-Path $repoRoot $WorkingDir)).Path
  if ($Env.Count -gt 0) {
    $setEnv = ($Env.GetEnumerator() | ForEach-Object { "`$env:$($_.Key)='$($_.Value)'" }) -join "; "
    $cmd = "$setEnv; Set-Location '$wd'; node $Script"
    $p = Start-Process powershell -ArgumentList @("-NoProfile", "-WindowStyle", "Hidden", "-Command", $cmd) -PassThru
  } else {
    $p = Start-Process -FilePath "node" -WorkingDirectory $wd -ArgumentList $Script -PassThru -WindowStyle Hidden
  }
  Start-Sleep -Seconds $WaitSeconds
  return $p
}

function Stop-Proc([System.Diagnostics.Process]$Proc) {
  if ($null -eq $Proc) { return }
  try { Stop-Process -Id $Proc.Id -Force -ErrorAction SilentlyContinue } catch {}
}

Write-Host "== Practice 19 (Postgres, port 5433) ==" -ForegroundColor Cyan
Push-Location (Join-Path $repoRoot "KR_4/practice_19_postgres_api")
docker compose up -d | Out-Null
Pop-Location

$p19 = $null
try {
  $p19 = Start-NodeService "KR_4/practice_19_postgres_api" "server.js"
  Write-Host ("  health: " + (Invoke-HttpStatus "http://localhost:3005/health"))
  Write-Host ("  docs:   " + (Invoke-HttpStatus "http://localhost:3005/docs"))
} finally {
  Stop-Proc $p19
}

Write-Host "== Practice 20 (Mongo, port 27018) ==" -ForegroundColor Cyan
Push-Location (Join-Path $repoRoot "KR_4/practice_20_mongo_api")
docker compose up -d | Out-Null
Pop-Location

$p20 = $null
try {
  $p20 = Start-NodeService "KR_4/practice_20_mongo_api" "server.js"
  Write-Host ("  health: " + (Invoke-HttpStatus "http://localhost:3006/health"))
  Write-Host ("  docs:   " + (Invoke-HttpStatus "http://localhost:3006/docs"))
} finally {
  Stop-Proc $p20
}

Write-Host "== Practice 21 (Redis + Practice 11 on :3020) ==" -ForegroundColor Cyan
docker rm -f redis-cache 2>$null | Out-Null
docker run -d --name redis-cache -p 6379:6379 redis | Out-Null

$p11 = $null
try {
  $p11 = Start-NodeService "KR_2/Practice_11/server" "app.js" @{ PORT = "3020" }
  Write-Host ("  health: " + (Invoke-HttpStatus "http://localhost:3020/health"))
  try {
    $login = Invoke-RestMethod -Uri "http://localhost:3020/api/auth/login" -Method POST `
      -Body '{"email":"admin@practice11.local","password":"admin123"}' -ContentType "application/json"
    $h1 = Invoke-WebRequest "http://localhost:3020/api/users" -Headers @{ Authorization = "Bearer $($login.accessToken)" } -UseBasicParsing
    $h2 = Invoke-WebRequest "http://localhost:3020/api/users" -Headers @{ Authorization = "Bearer $($login.accessToken)" } -UseBasicParsing
    Write-Host ("  cache:  " + $h1.Headers["X-Cache"] + " -> " + $h2.Headers["X-Cache"])
  } catch {
    Write-Host "  cache:  check failed (login or Redis)"
  }
} finally {
  Stop-Proc $p11
}

Write-Host "== Practice 23 (Docker Compose :8088) ==" -ForegroundColor Cyan
Push-Location (Join-Path $repoRoot "KR_4/practice_23_docker_compose")
docker compose up -d --build 2>&1 | Out-Null
Pop-Location
Write-Host ("  lb:     " + (Invoke-HttpStatus "http://localhost:8088/"))
try {
  $s1 = (Invoke-RestMethod "http://localhost:8088/").server
  $s2 = (Invoke-RestMethod "http://localhost:8088/").server
  Write-Host ("  rotate: $s1, $s2")
} catch {
  Write-Host "  rotate: n/a"
}

Write-Host ""
Write-Host "Practice 22: manual (3 backends + nginx on :8080) - see practice_22_load_balancer/README.md" -ForegroundColor Yellow
Write-Host "Done." -ForegroundColor Green
