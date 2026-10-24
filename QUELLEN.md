# BetQuote - Quellen

Laufender Index, wo Informationen waehrend der Arbeit an diesem Projekt tatsaechlich nachgeschlagen
wurden - siehe globale CLAUDE.md ("Sources file"). Kein Wissensdump, sondern eine Landkarte zu den
Quellen fuer spaetere Sessions.

## Infrastruktur/Setup (2026-09-21)

- **`C:\Users\Flux\.claude\ports.json`** - globale Port-Registry aller Projekte. BetQuote hat hier
  den Block `caddy-http=5190, caddy-https=5191, frontend=5192, backend=5193, postgres=5194`
  reserviert (naechster freier 10er-Block nach Milepoints Einzelports).
- **`G:\Projekte\WebApps\Milepoint\docker-compose.yml` / `start.ps1` / `Caddyfile`** - Referenz
  fuer Compose-Konventionen (Healthchecks, Container-Naming, `tls internal` mit
  `<projekt>.localhost`), Vorbild fuer BetQuotes eigene Dateien.
- **`G:\Projekte\WebApps\PrimeSource\scripts\git-hooks\post-commit`** - aktuellste Referenz fuer
  den post-commit-Hook inkl. automatischem Stempeln der CHANGELOG.dev.md-Ueberschrift mit
  Datum/Uhrzeit; BetQuotes Hook ist eine angepasste Variante davon (ohne "-dev"-Suffix in der
  Ueberschrift selbst, da das ein PrimeSource-spezifisches Detail war).
- **`https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json`** -
  offizielles .NET-Release-Metadaten-Feed, zeigt die jeweils aktuellste SDK/Runtime-Patchversion
  je Major-Channel (hier verwendet, um zu pruefen, ob die lokal installierte .NET-10-SDK-Version
  noch aktuell ist).
- **`npm view <paket> version`** - verwendet, um die jeweils aktuellste npm-Version von React,
  Vite, TypeScript, Tailwind CSS zu pruefen (statt sich auf das, was `npm create vite` gerade
  zufaellig aufgeloest hat, zu verlassen).

## Datenmodell/Migrationen (2026-09-21)

- **EF Core Code-First** gewaehlt statt reiner SQL-Migrationsdateien - `dotnet-ef` war bereits als
  globales Tool auf diesem Rechner installiert (`dotnet tool list --global`), was fuer dieses
  .NET-Backend-Grundgeruest die vorgesehene Konvention ist. Entities in
  `backend/Data/Entities/`, Fluent-API-Konfiguration (Indizes, Constraints, Praezision, JSONB) in
  `backend/Data/Configurations/` (ein `IEntityTypeConfiguration<T>` pro Entity), generierte
  Migrationen in `backend/Data/Migrations/`.
- **`EFCore.NamingConventions`** (Giorgi/efcore-Community-Paket) - mappt C#-PascalCase
  automatisch auf snake_case-SQL-Identifier (`UseSnakeCaseNamingConvention()` in
  `Program.cs`/`BetQuoteDbContextFactory.cs`), idiomatisch fuer Postgres statt jede Spalte
  einzeln per `HasColumnName()` umzubenennen.
- **Npgsql-JSONB-Mapping fuer `System.Text.Json.JsonDocument`** - Npgsql.EntityFrameworkCore.PostgreSQL
  unterstuetzt `JsonDocument` nativ als `jsonb`-Spaltentyp (`HasColumnType("jsonb")` reicht,
  kein eigener Value-Converter noetig) - verwendet fuer `Vorhersage.Ergebnisverteilung` und
  `Vorhersage.FaktorenSnapshot` (siehe `VorhersageConfiguration.cs`).
- **`backend/Data/BetQuoteDbContextFactory.cs`** - `IDesignTimeDbContextFactory`, noetig damit
  `dotnet ef migrations add/database update` ausserhalb des Docker-Containers auf diesem Windows-
  Rechner laeuft (Connection-String-Override per Env-Var `BETQUOTE_DESIGNTIME_CONNECTION`, damit
  das echte, per Docker-Compose-Secret verwaltete Postgres-Passwort nicht im Quellcode landen
  muss). Kommt zur Laufzeit im Container NIE zum Einsatz - dort registriert `Program.cs` den
  DbContext ganz regulaer ueber `AddDbContext`.
- Backup-/Restore-Skripte (`scripts/backup-db.ps1`, `scripts/restore-db.ps1`,
  `scripts/register-backup-task.ps1`) direkt gegen den laufenden `betquote-postgres-1`-Container
  getestet (siehe TECH_STACK.md "Datenhaltung & Backup -> Konkrete Umsetzung" fuer das Ergebnis).

## Backend-Kernlogik: Import/Vorhersage/Datenquellen (2026-09-21)

- **`https://api.openligadb.de/getavailableleagues`** - liefert alle bekannten Liga/Saison-
  Kombinationen; `getmatchdata/{liga}/{saison}` liefert bereits die KOMPLETTE Saison in einer
  Antwort (kein Pagination/Spieltag-Parameter noetig), `getmatchdata/{liga}` ohne Saison liefert
  den aktuell laufenden Spieltag - Basis fuer `OpenLigaDbClient`/`OpenLigaDbImportService`. Per
  `curl` live gegen die echte API verifiziert (u.a. `goals[].scoreTeam1/2` kann bei manchen
  aelteren Datensaetzen `null` statt einer Zahl sein - bl1 Saison 2010 beobachtet, siehe
  `OpenLigaDbDtos.cs`-Kommentar bei `FootballDataOrgGoalDto`/`OpenLigaDbGoalDto`).
- **`https://api.football-data.org/v4/competitions/PD`** - `PD` als football-data.org-Code fuer
  La Liga per `curl` verifiziert (403 wegen fehlendem Auth-Token bestaetigt bereits, dass der Code
  selbst existiert, ein falscher Code haette 404 geliefert). Volle API-Doku unter
  `https://docs.football-data.org/general/v4/` (nicht direkt abgerufen, DTOs anhand bekannter
  v4-Schema-Konventionen gebaut - bei echten Response-Abweichungen mit gesetztem API-Key
  nachjustieren).
- **`https://www.sportschau.de/audio/`** - Aufbau der Audio-Uebersichtsseite per `curl` inspiziert:
  jeder Audioplayer-Block traegt ein `data-v`-HTML-Attribut mit eingebettetem JSON (Titel,
  `broadcastedOnDateTime`, Permalink unter `pluginData.sharing@web.link`) - robuster
  Ansatzpunkt fuer `SportschauScraperService` als reines Text-Scraping der sichtbaren Seite.
  Keine offizielle API, Struktur kann sich jederzeit aendern (siehe TECH_STACK.md).
