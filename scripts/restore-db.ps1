<#
  BetQuote - PostgreSQL-Restore.

  Spielt einen mit backup-db.ps1 erzeugten Dump (Custom-Format) in eine laufende Datenbank
  zurueck - per Default in eine SEPARATE Test-Datenbank (-TargetDb), nicht ungefragt in die
  echte laufende DB, siehe CLAUDE.md ("vor jeder destruktiven/schwer umkehrbaren Aktion klar
  markieren und bestaetigen lassen"). Ein Restore direkt in die produktiv genutzte Datenbank
  ueberschreibt/loescht bestehende Daten und erfordert -Force.

  Ausfuehrung:
    .\scripts\restore-db.ps1 -DumpFile backups\betquote-dev-2026-09-21_201900.dump
        Spielt den Dump in eine neue Datenbank "betquote_restore_test" auf demselben
        Postgres-Server ein (Default -TargetDb) - sicherer Weg, um einen Dump zu verifizieren,
        ohne die echte Datenbank anzufassen.

    .\scripts\restore-db.ps1 -DumpFile <pfad> -TargetDb betquote -Force
        Spielt den Dump DIREKT in die echte Datenbank zurueck (loescht/ueberschreibt bestehende
        Daten dort) - nur mit explizitem -Force, s.o.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$DumpFile,

    [ValidateSet('dev', 'prod')]
    [string]$Env = 'dev',

    [string]$TargetDb = 'betquote_restore_test',

    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$ProjectName = if ($Env -eq 'prod') { 'betquote-live' } else { 'betquote' }
$RootDir = Split-Path -Parent $PSScriptRoot
$ContainerName = "$ProjectName-postgres-1"

$EnvFile = Join-Path $RootDir '.env'
function Get-EnvValue([string]$Path, [string]$Key, [string]$Default) {
    if (-not (Test-Path $Path)) { return $Default }
    $line = Get-Content -Path $Path | Where-Object { $_ -match "^\s*$Key\s*=" } | Select-Object -First 1
    if (-not $line) { return $Default }
    return ($line -split '=', 2)[1].Trim()
}
$PostgresUser = Get-EnvValue -Path $EnvFile -Key 'POSTGRES_USER' -Default 'betquote'
$PostgresDb = Get-EnvValue -Path $EnvFile -Key 'POSTGRES_DB' -Default 'betquote'

if (-not (Test-Path $DumpFile)) { throw "Dump-Datei nicht gefunden: $DumpFile" }
$DumpFile = (Resolve-Path $DumpFile).Path

$running = & docker ps --filter "name=^/$ContainerName$" --format '{{.Names}}' 2>$null
if (-not $running) {
    throw "Container '$ContainerName' laeuft nicht - Restore abgebrochen. Erst '.\start.ps1 -Env $Env' ausfuehren."
}

$isRealDatabase = ($TargetDb -eq $PostgresDb)
if ($isRealDatabase -and -not $Force) {
    throw "TargetDb '$TargetDb' ist die echte, produktiv genutzte Datenbank - das ueberschreibt bestehende Daten dort. Zum Bestaetigen -Force anhaengen, oder -TargetDb weglassen, um stattdessen gegen die Test-Datenbank 'betquote_restore_test' zu pruefen."
}

Write-Host "Kopiere Dump in den Container..." -ForegroundColor Cyan
$dumpFileName = Split-Path -Leaf $DumpFile
$dumpPathInContainer = "/tmp/$dumpFileName"
& docker cp $DumpFile "${ContainerName}:${dumpPathInContainer}"
if ($LASTEXITCODE -ne 0) { throw "Kopieren des Dumps in den Container ist fehlgeschlagen (Exit Code $LASTEXITCODE)." }

try {
    if ($isRealDatabase) {
        Write-Host "ACHTUNG: Restore direkt in '$TargetDb' - bestehende Daten werden ueberschrieben/geloescht." -ForegroundColor Red
    }

    # Zieldatenbank anlegen, falls sie noch nicht existiert (Testdatenbank-Fall) - "IF NOT EXISTS"
    # gibt es bei CREATE DATABASE nicht, daher ueber pg_database pruefen.
    $dbExists = & docker exec $ContainerName psql -U $PostgresUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$TargetDb';"
    if (-not ($dbExists -match '1')) {
        Write-Host "Lege Zieldatenbank '$TargetDb' an..." -ForegroundColor Cyan
        & docker exec $ContainerName psql -U $PostgresUser -d postgres -c "CREATE DATABASE $TargetDb;"
        if ($LASTEXITCODE -ne 0) { throw "Anlegen der Zieldatenbank '$TargetDb' ist fehlgeschlagen." }
    }
    elseif ($isRealDatabase) {
        # --clean --if-exists laesst pg_restore vor dem Restore bestehende Objekte droppen -
        # noetig, damit ein wiederholter Restore in dieselbe (bereits befuellte) DB nicht an
        # "already exists"-Fehlern scheitert.
        Write-Host "Zieldatenbank existiert bereits - bestehende Objekte werden vor dem Restore entfernt (--clean)." -ForegroundColor Yellow
    }

    Write-Host "Spiele Dump in '$TargetDb' ein..." -ForegroundColor Cyan
    & docker exec $ContainerName pg_restore -U $PostgresUser -d $TargetDb --clean --if-exists --no-owner --no-privileges $dumpPathInContainer
    if ($LASTEXITCODE -ne 0) { throw "pg_restore ist fehlgeschlagen (Exit Code $LASTEXITCODE)." }

    Write-Host "`nRestore abgeschlossen. Verifikation:" -ForegroundColor Green
    & docker exec $ContainerName psql -U $PostgresUser -d $TargetDb -c "SELECT schemaname, count(*) AS tabellen FROM pg_tables WHERE schemaname = 'public' GROUP BY schemaname;"

    if (-not $isRealDatabase) {
        Write-Host "`nTest-Datenbank '$TargetDb' aufraeumen mit:" -ForegroundColor Cyan
        Write-Host "  docker exec $ContainerName psql -U $PostgresUser -d postgres -c ""DROP DATABASE $TargetDb;"""
    }
}
finally {
    & docker exec $ContainerName rm -f $dumpPathInContainer
}
