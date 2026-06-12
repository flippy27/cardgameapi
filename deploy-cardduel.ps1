# One-command deploy of CardDuel.ServerApi to the Raspberry Pi.
#   .\deploy-cardduel.ps1
# Packs the source (no bin/obj/logs/.git), copies it to the Pi over SSH, and
# rebuilds + restarts the api + postgres + redis (NOT its nginx — the Pi's
# Caddy / dnsmasq fronts it). Uses the dedicated key ~/.ssh/notes_pi.
param(
  [string]$PiHost = "flippy@192.168.1.87",
  [string]$Key    = "$env:USERPROFILE\.ssh\notes_pi"
)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$tgz  = Join-Path $env:TEMP "cardduel_deploy.tgz"

Write-Host "==> Packing CardDuel source..." -ForegroundColor Cyan
tar --exclude='*/bin' --exclude='*/obj' --exclude='./bin' --exclude='./obj' `
    --exclude='*/logs' --exclude='./logs' --exclude='.git' --exclude='*.log' `
    -czf $tgz -C $root .

Write-Host "==> Copying to $PiHost..." -ForegroundColor Cyan
scp -i $Key -o StrictHostKeyChecking=accept-new $tgz "${PiHost}:/tmp/cardduel.tgz"

Write-Host "==> Building + starting on the Pi (first .NET build is slow on ARM)..." -ForegroundColor Cyan
$remote = @'
set -e
mkdir -p ~/cardduel
tar xzf /tmp/cardduel.tgz -C ~/cardduel
rm -f /tmp/cardduel.tgz
cd ~/cardduel
# Start only api + its db + redis; skip the bundled nginx (Pi's Caddy fronts it).
docker compose up -d --build api postgres redis
docker compose ps
'@
# Docker writes build progress to stderr; don't let PowerShell treat that as a
# fatal error. Merge streams and check the real exit code instead.
$ErrorActionPreference = "Continue"
ssh -i $Key $PiHost $remote 2>&1 | ForEach-Object { "$_" }
if ($LASTEXITCODE -ne 0) { throw "Remote build failed (exit $LASTEXITCODE)" }

Remove-Item $tgz -ErrorAction SilentlyContinue
Write-Host "==> Done -> http://flippy.cardserver (o http://192.168.1.87:5000)" -ForegroundColor Green
