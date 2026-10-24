<#
  BetQuote - richtet einen taeglichen Windows Scheduled Task fuer das DB-Backup ein.

  Windows Task Scheduler statt Cronjob im Postgres-Container: der Stack laeuft auf diesem
  Windows-Rechner (Docker Desktop), ein Cronjob INNERHALB des Alpine-basierten Postgres-Images
  muesste zusaetzlich erst installiert/persistiert werden und liefe nur, waehrend der Container
  laeuft - der native, bereits vorhandene Windows Task Scheduler auf dem Host ist hier die
  einfachere und robustere Entsprechung (aehnlich wie start.ps1 selbst ein Host-Skript ist, kein
  Container-Skript). Der Task ruft scripts/backup-db.ps1 auf, welches selbst prueft, ob der
  Postgres-Container laeuft.

  Einmalig ausfuehren (kann auch bei jedem start.ps1-Lauf sicher erneut ausgefuehrt werden -
  ueberschreibt eine bestehende gleichnamige Aufgabe mit denselben Einstellungen):
    .\scripts\register-backup-task.ps1

  Task entfernen:
    Unregister-ScheduledTask -TaskName "BetQuote-DB-Backup" -Confirm:$false
#>

param(
    [string]$TaskName = 'BetQuote-DB-Backup',
    [string]$Time = '03:30'
)

$ErrorActionPreference = 'Stop'
$RootDir = Split-Path -Parent $PSScriptRoot
$BackupScript = Join-Path $PSScriptRoot 'backup-db.ps1'

$action = New-ScheduledTaskAction `
    -Execute 'powershell.exe' `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$BackupScript`" -Env dev" `
    -WorkingDirectory $RootDir

$trigger = New-ScheduledTaskTrigger -Daily -At $Time

$settings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -DontStopOnIdleEnd `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 30) `
    -MultipleInstances IgnoreNew

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Description 'Taegliches pg_dump-Backup der BetQuote-Datenbank (siehe scripts/backup-db.ps1, TECH_STACK.md "Datenhaltung & Backup").' `
    -Force | Out-Null

Write-Host "Scheduled Task '$TaskName' eingerichtet - taeglich um $Time Uhr." -ForegroundColor Green
Write-Host "Voraussetzung: der BetQuote-Dev-Stack muss zum Zeitpunkt $Time laufen (Postgres-Container), sonst bricht backup-db.ps1 mit einer klaren Fehlermeldung ab, statt ein leeres/fehlerhaftes Backup zu erzeugen."
Write-Host "Manuell pruefen/testen: Start-ScheduledTask -TaskName '$TaskName'"
