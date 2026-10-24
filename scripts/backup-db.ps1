<#
  BetQuote - PostgreSQL-Backup.

  Erzeugt einen pg_dump der laufenden BetQuote-Datenbank (Custom-Format, komprimiert,
  pg_restore-faehig) und legt ihn AUSSERHALB des Docker-Volumes ab, unter
  <Repo-Root>\backups\ (per .gitignore vom Repo ausgeschlossen - siehe TECH_STACK.md
  "Datenhaltung & Backup"). Alte Dumps werden nach Ablauf von -RetentionDays automatisch
  geloescht (Default: 30 Tage).

  Ausfuehrung:
    .\scripts\backup-db.ps1                     Backup vom Dev-Stack (Default -Env dev)
    .\scripts\backup-db.ps1 -Env prod            Backup vom Prod-Stack
    .\scripts\backup-db.ps1 -RetentionDays 14    Abweichende Aufbewahrungsdauer

  Automatisierung: siehe scripts/register-backup-task.ps1 - richtet einen taeglichen Windows
  Scheduled Task ein, der genau dieses Skript aufruft (kein Cronjob im Container moeglich, da
  Windows-Host ohne Cron - der Windows Task Scheduler ist hier die native Entsprechung).

  Restore-Weg: siehe scripts/restore-db.ps1 sowie TECH_STACK.md "Datenhaltung & Backup".
#>

param(
    [ValidateSet('dev', 'prod')]
    [string]$Env = 'dev',

    [int]$RetentionDays = 30
)

$ErrorActionPreference = 'Stop'
$ProjectName = if ($Env -eq 'prod') { 'betquote-live' } else { 'betquote' }
$RootDir = Split-Path -Parent $PSScriptRoot
$BackupDir = Join-Path $RootDir 'backups'
$ContainerName = "$ProjectName-postgres-1"

$EnvFile = Join-Path $RootDir '.env'

function Get-EnvValue([string]$Path, [string]$Key, [string]$Default) {
    if (-not (Test-Path $Path)) { return $Default }
    $line = Get-Content -Path $Path | Where-Object { $_ -match "^\s*$Key\s*=" } | Select-Object -First 1
    if (-not $line) { return $Default }
    return ($line -split '=', 2)[1].Trim()
}

$PostgresDb = Get-EnvValue -Path $EnvFile -Key 'POSTGRES_DB' -Default 'betquote'
$PostgresUser = Get-EnvValue -Path $EnvFile -Key 'POSTGRES_USER' -Default 'betquote'

if (-not (Test-Path $BackupDir)) {
    New-Item -Path $BackupDir -ItemType Directory -Force | Out-Null
}

$running = & docker ps --filter "name=^/$ContainerName$" --format '{{.Names}}' 2>$null
if (-not $running) {
    throw "Container '$ContainerName' laeuft nicht - Backup abgebrochen. Erst '.\start.ps1 -Env $Env' ausfuehren."
}

$timestamp = Get-Date -Format 'yyyy-MM-dd_HHmmss'
$dumpFileName = "betquote-$Env-$timestamp.dump"
$dumpPathInContainer = "/tmp/$dumpFileName"
$dumpPathOnHost = Join-Path $BackupDir $dumpFileName

Write-Host "Erzeuge Backup von '$PostgresDb' ($Env) ..." -ForegroundColor Cyan

# Custom-Format (-F c): komprimiert, pg_restore-faehig, erlaubt selektiven/parallelen Restore -
# im Gegensatz zu reinem SQL-Text-Dump (siehe pg_dump-Doku). Dump entsteht im Container-eigenen
# /tmp (nicht im Docker-Volume selbst), wird danach auf den Host kopiert und im Container wieder
# geloescht - der Dump landet damit nur einmalig ausserhalb des Volumes auf dem Host.
& docker exec $ContainerName pg_dump -U $PostgresUser -d $PostgresDb -F c -f $dumpPathInContainer
if ($LASTEXITCODE -ne 0) { throw "pg_dump ist fehlgeschlagen (Exit Code $LASTEXITCODE)." }

& docker cp "${ContainerName}:${dumpPathInContainer}" $dumpPathOnHost
if ($LASTEXITCODE -ne 0) { throw "Kopieren des Dumps aus dem Container ist fehlgeschlagen (Exit Code $LASTEXITCODE)." }

& docker exec $ContainerName rm -f $dumpPathInContainer

$size = (Get-Item $dumpPathOnHost).Length
$sizeH = if ($size -gt 1MB) { "{0:N1} MB" -f ($size / 1MB) } else { "{0:N1} KB" -f ($size / 1KB) }
Write-Host "Backup erstellt: $dumpPathOnHost ($sizeH)" -ForegroundColor Green

# Rotation: alte Dumps desselben Env-Prefixes ueber der Aufbewahrungsdauer loeschen.
$cutoff = (Get-Date).AddDays(-$RetentionDays)
$oldDumps = Get-ChildItem -Path $BackupDir -Filter "betquote-$Env-*.dump" |
    Where-Object { $_.LastWriteTime -lt $cutoff }

if ($oldDumps) {
    Write-Host "Entferne $($oldDumps.Count) Backup(s) aelter als $RetentionDays Tage..." -ForegroundColor Yellow
    $oldDumps | Remove-Item -Force
}

$remaining = (Get-ChildItem -Path $BackupDir -Filter "betquote-$Env-*.dump").Count
Write-Host "Aktuell $remaining Backup(s) fuer '$Env' in $BackupDir." -ForegroundColor Cyan
