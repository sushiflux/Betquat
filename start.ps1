<#
  BetQuote Docker-Stack - Start/Stopp/Status in einer Datei.

  .\start.ps1                        Startet den Dev-Stack (Default -Env dev)
  .\start.ps1 -Env prod               Startet den Prod-Stack (siehe CLAUDE.md "Dev/live
                                       environment separation" - noch nicht vollstaendig
                                       funktionsfaehig, siehe docker-compose.prod.yml)
  .\start.ps1 -Action stop            Stoppt den Stack (Docker-Volumes/Daten bleiben erhalten)
  .\start.ps1 -Action restart         Stoppt und startet neu
  .\start.ps1 -Action status          Zeigt den Container-Status
  .\start.ps1 -Action logs            Folgt den Logs aller Services

  Secrets (z.B. das Postgres-Passwort) werden beim allerersten Lauf automatisch als echte
  Zufallswerte unter ./secrets/ generiert - siehe CLAUDE.md "Secrets management fuer lokale
  Docker Compose Stacks". Nichts davon landet in .env oder im Git-Repo.
#>

param(
    [ValidateSet('start', 'stop', 'restart', 'status', 'logs')]
    [string]$Action = 'start',

    [ValidateSet('dev', 'prod')]
    [string]$Env = 'dev',

    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$ProjectName = if ($Env -eq 'prod') { 'betquote-live' } else { 'betquote' }
$RootDir = $PSScriptRoot
Set-Location $RootDir

$EnvFile = Join-Path $RootDir '.env'
$EnvExampleFile = Join-Path $RootDir '.env.example'
$SecretsDir = Join-Path $RootDir 'secrets'
$PostgresPasswordFile = Join-Path $SecretsDir 'postgres_password.txt'
$OverrideFile = Join-Path $RootDir "docker-compose.$Env.yml"

function New-RandomToken([int]$Length = 32) {
    # RNGCryptoServiceProvider statt RandomNumberGenerator.Fill(): Letzteres existiert erst ab
    # .NET Core/PS7, dieses Skript laeuft aber unter der auf Windows vorinstallierten Windows
    # PowerShell 5.1 (.NET Framework) - RNGCryptoServiceProvider ist dort verfuegbar und
    # ebenfalls ein kryptographisch sicherer RNG.
    $bytes = [byte[]]::new($Length * 2)
    $rng = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    # Base64 -> alphanumerisch bereinigen, damit der Wert in Compose-Secrets-Dateien und
    # Connection-Strings ohne Escaping-Probleme (Sonderzeichen wie '/', '+', '=') nutzbar ist.
    # Doppelte Byte-Laenge vor dem Bereinigen, damit nach dem Herausfiltern von Sonderzeichen
    # sicher genug Zeichen fuer die gewuenschte Laenge uebrig bleiben.
    $clean = [Convert]::ToBase64String($bytes) -replace '[^a-zA-Z0-9]', ''
    return $clean.Substring(0, $Length)
}

function Get-AppVersion {
    # vYear.Month.CommitOfTheYear, kein Zero-Padding (siehe globale CLAUDE.md "Versioning
    # scheme"). version.txt selbst wird vom post-commit-Hook gepflegt - hier nur zur Anzeige
    # nochmal frisch berechnet, falls der Hook noch nie gelaufen ist (allererster Start vor dem
    # ersten Commit).
    $versionFile = Join-Path $RootDir 'version.txt'
    if (Test-Path $versionFile) {
        $content = (Get-Content $versionFile -Raw).Trim()
        if ($content) { return $content }
    }
    return 'v0.0.0'
}

function Resolve-GitExe {
    $gitExe = (Get-Command git -ErrorAction SilentlyContinue).Source
    if (-not $gitExe) {
        $fallback = 'C:\Program Files\Git\cmd\git.exe'
        if (Test-Path $fallback) { $gitExe = $fallback }
    }
    return $gitExe
}

function Set-GitHooksPath {
    # Wired scripts/git-hooks (im Repo getrackt) automatisch ein, damit post-commit version.txt
    # nach jedem Commit aktuell haelt - ohne manuellen Setup-Schritt (siehe globale CLAUDE.md).
    $gitExe = Resolve-GitExe
    if (-not $gitExe) { return }
    try { & $gitExe -C $RootDir config core.hooksPath 'scripts/git-hooks' 2>$null } catch {}
}

function Set-EnvValue([string]$Path, [string]$Key, [string]$Value) {
    if (-not (Test-Path $Path)) { New-Item -Path $Path -ItemType File -Force | Out-Null }
    $lines = Get-Content -Path $Path -ErrorAction SilentlyContinue
    $pattern = "^\s*$Key\s*="
    $found = $false
    $updated = foreach ($line in $lines) {
        if ($line -match $pattern) { $found = $true; "$Key=$Value" }
        else { $line }
    }
    if (-not $found) { $updated = @($updated) + "$Key=$Value" }
    Set-Content -Path $Path -Value $updated -Encoding utf8
}

function Get-EnvValue([string]$Path, [string]$Key) {
    if (-not (Test-Path $Path)) { return $null }
    $line = Get-Content -Path $Path | Where-Object { $_ -match "^\s*$Key\s*=" } | Select-Object -First 1
    if (-not $line) { return $null }
    return ($line -split '=', 2)[1].Trim()
}

function Initialize-Secrets {
    # Echte Zufalls-Secrets statt Platzhalter (siehe CLAUDE.md "Secrets management fuer lokale
    # Docker Compose Stacks") - werden genau einmal generiert und danach nie ueberschrieben,
    # damit ein Neustart nicht das bestehende DB-Passwort aendert (sonst kann Postgres sein
    # eigenes gespeichertes Passwort nicht mehr matchen).
    if (-not (Test-Path $SecretsDir)) {
        New-Item -Path $SecretsDir -ItemType Directory -Force | Out-Null
    }
    if (-not (Test-Path $PostgresPasswordFile)) {
        Write-Host "Kein Postgres-Passwort gefunden - generiere ein neues zufaelliges Secret." -ForegroundColor Yellow
        $password = New-RandomToken 32
        Set-Content -Path $PostgresPasswordFile -Value $password -NoNewline -Encoding utf8
    }
}

function Initialize-EnvFile {
    if (-not (Test-Path $EnvFile)) {
        Write-Host "Keine .env gefunden - erstelle sie aus .env.example." -ForegroundColor Yellow
        Copy-Item $EnvExampleFile $EnvFile
    }
}

function Test-TcpPortFree([int]$Port) {
    # Get-NetTCPConnection statt eines eigenen TcpListener-Bind-Versuchs - spiegelt die
    # tatsaechliche Listening-Socket-Tabelle wider (wie netstat), siehe globale CLAUDE.md
    # "Port registry": kein stiller Auto-Fallback mehr, ein belegter Port ist ein echter
    # Konflikt und wird laut gemeldet.
    return -not (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
}

$PortRegistryFile = 'C:\Users\Flux\.claude\ports.json'

function Resolve-Ports {
    $portMap = @{
        'CADDY_HTTP_PORT'  = 'caddy-http'
        'CADDY_HTTPS_PORT' = 'caddy-https'
        'FRONTEND_PORT'    = 'frontend'
        'BACKEND_PORT'     = 'backend'
        'POSTGRES_PORT'    = 'postgres'
    }

    if (-not (Test-Path $PortRegistryFile)) {
        throw "Port-Registry nicht gefunden: $PortRegistryFile - siehe 'Port registry' in der globalen CLAUDE.md."
    }
    $registry = Get-Content $PortRegistryFile -Raw | ConvertFrom-Json
    if (-not $registry.betquote) {
        throw "Kein 'betquote'-Eintrag in $PortRegistryFile gefunden."
    }
    $projectPorts = $registry.betquote

    foreach ($key in $portMap.Keys) {
        $regKey = $portMap[$key]
        $port = $projectPorts.$regKey
        if (-not $port) {
            throw "Kein Port fuer '$regKey' ($key) in $PortRegistryFile hinterlegt."
        }
        $port = [int]$port

        if (-not (Test-TcpPortFree $port)) {
            throw "Port $port ($key) ist belegt (fremder Prozess oder ein anderes Projekt). Registrierter Port laut $PortRegistryFile - Konflikt bitte manuell aufloesen, keine automatische Ausweichung mehr."
        }

        Set-EnvValue -Path $EnvFile -Key $key -Value $port
    }
}

function Invoke-Compose([string[]]$ComposeArgs) {
    & docker compose -p $ProjectName -f docker-compose.yml -f $OverrideFile --env-file $EnvFile @ComposeArgs
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose $($ComposeArgs -join ' ') ist fehlgeschlagen (Exit Code $LASTEXITCODE)."
    }
}

function Get-ComposeContainers {
    try {
        $raw = (& docker compose -p $ProjectName -f docker-compose.yml -f $OverrideFile ps --format json 2>$null) -join "`n"
        if (-not $raw.Trim()) { return @() }
        try {
            $parsed = $raw | ConvertFrom-Json -ErrorAction Stop
            if ($parsed -is [System.Array]) { return $parsed }
            return @($parsed)
        }
        catch {
            $containers = @()
            foreach ($line in ($raw -split "`n")) {
                if ($line.Trim()) {
                    try { $containers += ($line | ConvertFrom-Json -ErrorAction Stop) } catch {}
                }
            }
            return $containers
        }
    }
    catch {
        return @()
    }
}

function Wait-ForHealth([int]$TimeoutSeconds = 180) {
    Write-Host "`nWarte auf Healthchecks..." -ForegroundColor Cyan
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $servicesWithHealthcheck = @('postgres', 'backend')
    if ($Env -eq 'dev') { $servicesWithHealthcheck += 'frontend' }

    while ((Get-Date) -lt $deadline) {
        $containers = Get-ComposeContainers
        $pending = @()
        $unhealthy = @()

        foreach ($service in $servicesWithHealthcheck) {
            $c = $containers | Where-Object { $_.Service -eq $service } | Select-Object -First 1
            if (-not $c) { $pending += $service; continue }
            if ($c.Health -eq 'unhealthy') { $unhealthy += $service }
            elseif ($c.Health -and $c.Health -ne 'healthy') { $pending += $service }
            elseif (-not $c.Health -and $c.State -ne 'running') { $pending += $service }
        }

        if ($unhealthy.Count -gt 0) {
            throw "Diese Services sind unhealthy: $($unhealthy -join ', '). Pruefe: docker compose -p $ProjectName logs <service>"
        }

        if ($pending.Count -eq 0) {
            Write-Host "Alle Services sind healthy." -ForegroundColor Green
            return
        }

        Write-Host "  ...warte noch auf: $($pending -join ', ')"
        Start-Sleep -Seconds 3
    }

    throw "Timeout beim Warten auf Healthchecks nach $TimeoutSeconds Sekunden. Pruefe: docker compose -p $ProjectName logs"
}

function Test-CertificateTrusted([System.Security.Cryptography.X509Certificates.X509Certificate2]$Cert) {
    return [bool](Get-ChildItem Cert:\CurrentUser\Root | Where-Object { $_.Thumbprint -eq $Cert.Thumbprint })
}

function Install-CaddyRootCertificate {
    # Caddy signiert "tls internal"-Zertifikate mit einer selbst erzeugten lokalen CA. Ohne
    # einmaligen Trust-Import warnt jeder Browser bei https://betquote.localhost - das hier holt
    # das automatisch nach (kein Admin noetig, nur aktueller Windows-Benutzer).
    $ErrorActionPreference = 'Continue'
    $tempCert = Join-Path $env:TEMP "betquote-caddy-root-$PID.crt"
    try {
        & docker compose -p $ProjectName -f docker-compose.yml -f $OverrideFile --env-file $EnvFile cp "caddy:/data/caddy/pki/authorities/local/root.crt" $tempCert 2>$null
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path $tempCert)) {
            Write-Warning "Caddy-Root-Zertifikat konnte noch nicht gelesen werden - HTTPS-Warnung im Browser ggf. manuell akzeptieren."
            return
        }

        $cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($tempCert)
        if (Test-CertificateTrusted $cert) { return }

        Write-Host "`nImportiere lokales Caddy-HTTPS-Zertifikat in den Windows-Zertifikatspeicher (nur aktueller Benutzer)..." -ForegroundColor Yellow
        & certutil -user -addstore -f "ROOT" $tempCert | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Zertifikat importiert - Browser sollte https://betquote.localhost jetzt ohne Warnung akzeptieren." -ForegroundColor Green
        }
        else {
            Write-Warning "certutil-Import fehlgeschlagen (Exit Code $LASTEXITCODE)."
        }
    }
    finally {
        Remove-Item $tempCert -ErrorAction SilentlyContinue
    }
    $ErrorActionPreference = 'Stop'
}

if ($Action -eq 'restart') {
    Invoke-Compose @('down')
    $Action = 'start'
}

switch ($Action) {
    'stop' {
        Write-Host "Stoppe BetQuote-Stack ($Env)..." -ForegroundColor Cyan
        Invoke-Compose @('down')
        Write-Host "Gestoppt. Datenbank-Volumes bleiben erhalten." -ForegroundColor Green
        exit 0
    }
    'status' {
        Invoke-Compose @('ps')
        exit 0
    }
    'logs' {
        Invoke-Compose @('logs', '-f', '--tail=100')
        exit 0
    }
}

Initialize-EnvFile
Initialize-Secrets

Write-Host "Beende evtl. laufende eigene BetQuote-Container ($Env)..." -ForegroundColor Cyan
Invoke-Compose @('down', '--remove-orphans')

Write-Host "Pruefe Ports..." -ForegroundColor Cyan
Resolve-Ports

Set-GitHooksPath

$appVersion = Get-AppVersion

$upArgs = @('up', '-d')
if (-not $NoBuild) { $upArgs += '--build' }

Write-Host "Starte BetQuote-Stack ($Env)..." -ForegroundColor Cyan
Invoke-Compose $upArgs

Wait-ForHealth

$caddyHttpPort = Get-EnvValue -Path $EnvFile -Key 'CADDY_HTTP_PORT'
$caddyHttpsPort = Get-EnvValue -Path $EnvFile -Key 'CADDY_HTTPS_PORT'

Install-CaddyRootCertificate

Write-Host "`nBetQuote-Stack laeuft ($appVersion, Env=$Env):" -ForegroundColor Green
Write-Host "  App:      https://localhost:$caddyHttpsPort  (auch: https://betquote.localhost:$caddyHttpsPort - http://localhost:$caddyHttpPort leitet automatisch um)"
Write-Host "`nBeim ersten Aufruf warnt der Browser eventuell kurz vor dem Zertifikat (selbst ausgestellte, lokale CA von Caddy) - sollte nach dem automatischen Import oben nicht mehr noetig sein."
Write-Host "`nStoppen mit:  .\start.ps1 -Action stop -Env $Env"
Write-Host "Status mit:   .\start.ps1 -Action status -Env $Env"
Write-Host "Logs mit:     .\start.ps1 -Action logs -Env $Env"
