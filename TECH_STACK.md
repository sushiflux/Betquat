# BetQuote – Systeme & Technologien im Überblick

Diese Datei fasst zusammen, welche Systeme, Technologien und externen Datenquellen für BetQuote
eingesetzt werden — als schneller Überblick, ohne dass man dafür PREDICTION_FACTORS.md,
APP_STRUCTURE.md und die Docker-Konfiguration selbst durchsuchen muss. Details/Begründungen zu den
Datenquellen stehen in [PREDICTION_FACTORS.md](PREDICTION_FACTORS.md) (Abschnitt "Datenquellen"),
Details zur Seitenstruktur in [APP_STRUCTURE.md](APP_STRUCTURE.md).

---

## Anwendungs-Stack

| Bereich | Technologie | Zweck |
|---|---|---|
| Backend | .NET / ASP.NET Core | API-Endpoints, Datenimport-Jobs, Vorhersagemodell-Berechnung |
| Frontend | React + Vite + TypeScript + Tailwind CSS | Web-Oberfläche (Spieltag-Feed, Detailseiten, Statistik) |
| Datenbank | PostgreSQL | Persistente Speicherung aller Spiel-/Team-/Vorhersagedaten |
| Reverse Proxy | Caddy | Lokales HTTPS (`betquote.localhost`), Routing zwischen Frontend/Backend |
| Betrieb | Docker Compose (dev/prod-Profile getrennt) | Lokaler Stack auf diesem Windows-Rechner, `start.ps1` als Einstiegspunkt |
| Animation | `motion` (npm) | Sanfte Übergänge/Transitions im Frontend |
| Icons | Iconify / Phosphor / Tabler (je nach Bedarf) | UI-Icons statt selbst gezeichneter Grafiken |

Kein Login/Auth-System — BetQuote ist ein rein privates Ein-Personen-Tool ohne öffentlichen Zugriff.

---

## Externe Datenquellen

Ausführliche Begründung, Abdeckung und Einschränkungen je Quelle stehen in PREDICTION_FACTORS.md.
Kurzüberblick:

**Ziel-Wettbewerbe aktualisiert** (Nutzer-Entscheidung): 1. Bundesliga, 2. Bundesliga, **La Liga**
— 3. Liga und DFB-Pokal wurden wieder rausgenommen.

| Quelle | Wofür | Status |
|---|---|---|
| **OpenLigaDB** | Primärquelle für die deutschen Wettbewerbe: Spiele, Ergebnisse, Live-Stand, Tore mit Minute, Team-Logos, Tabellen — für 1./2. Bundesliga | Aktiv, harte Abhängigkeit (für 1./2. Bundesliga) |
| **football-data.org** | Primärquelle für **La Liga** (neu) — OpenLigaDB deckt La Liga nur über ein unzuverlässiges, wechselndes Community-Schema ab, football-data.org hat dafür ein stabiles Schema im kostenlosen Tier (12 Top-Wettbewerbe, 10 Req/Min) | Aktiv, harte Abhängigkeit (für La Liga) — Live-Scores dort leicht verzögert statt nahezu live, für dieses Vorhersage-Tool unkritisch |
| **Kaggle "AllBundesligaGamesEver"** | Einmaliger historischer Import, 1. Liga ab 1963 | Aktiv, einmaliger Import |
| **Understat** (Scraping) | xG/xGA-Anreicherung, nur 1. Liga, ab 2014/15 | Aktiv, optional/austauschbar |
| **Sofascore** (inoffizielle API) | Ballbesitz/Schüsse pro Halbzeit, xGOT pro Schuss | Aktiv, optional/austauschbar (undokumentiertes Drittanbieter-API) |
| **FBref** (Scraping) | Ergänzende Team-/Spielerstatistiken (xG, PSxG/xGOT, Progressive Passes) | Aktiv, optional/austauschbar |
| **ARD Sportschau Audiostreams** (Scraping) | Radio-/Audiostream-Direktlinks pro Spiel (Einzelspiel + Konferenz), kein Vorhersage-Faktor, reines UI-Komfort-Feature (siehe APP_STRUCTURE.md) | Aktiv, optional/austauschbar — keine offizielle API gefunden, Einzelspiel-Links ändern sich pro Spieltag (keine vorhersagbare URL-Struktur), daher Scraping der Sportschau-Übersichtsseite nötig; deckt nur die deutschen Wettbewerbe ab, nicht La Liga |

Geprüft, aber aktuell nicht eingeplant: StatsBomb Open Data, API-Football
(siehe PREDICTION_FACTORS.md für die jeweilige Begründung).

**Bewusst nicht eingeplant**: Value-Betting-/Arbitrage-Betting-Funktionalität (Live-Odds mehrerer
Buchmacher, Kapitaleinsatz-Rechner) — wurde mit dem Nutzer diskutiert und explizit verworfen, um
beim Charakter eines reinen Vorhersage-Tools zu bleiben (siehe PREDICTION_FACTORS.md Abschnitt
"Konfidenz-Markierung" für die Abgrenzung im Detail).

---

## Vorhersagemodell

1. **Poisson-/Dixon-Coles-Modell** — statistisches Grundgerüst für Torerwartung je Team, liefert
   direkt eine Wahrscheinlichkeitsverteilung über mögliche Ergebnisse (exaktes Ergebnis, 1X2,
   Over/Under, BTTS als gemeinsames Nebenprodukt derselben Berechnung)
2. **Gradient-Boosting-Modell** (LightGBM/XGBoost) — zweite Stufe auf den 🔴/🟡-Faktoren aus
   PREDICTION_FACTORS.md (Form, Elo-Rating, Verletzungen, Tabellensituation, xG/xGOT, etc.)
3. Eigenes **Elo-Rating** pro Team, laufend aus den Ergebnisdaten selbst berechnet (keine externe
   Quelle nötig)

---

## Datenhaltung & Backup

**PostgreSQL ist die alleinige "Source of Truth" für alle importierten und berechneten Daten**
(Spiele, Teams, Vorhersagen, Elo-Historie, Trefferquote-Auswertungen). Das ist bewusst so gebaut,
damit Langzeitstatistiken (Trefferquote über Spieltage/Saisons hinweg, siehe APP_STRUCTURE.md
Abschnitt 5) nicht verloren gehen, wenn der Docker-Stack neu aufgesetzt, ein Container gelöscht
oder der Rechner neu eingerichtet wird.

- **Datenbank-Daten liegen in einem benannten Docker-Volume** (nicht im Container selbst) —
  übersteht Container-Neustarts/-Neuerstellungen von sich aus, aber **kein Ersatz für ein echtes
  Backup** (ein gelöschtes Volume oder ein defekter Datenträger sind damit nicht abgesichert)
- **Regelmäßiges Backup nötig** (vom Nutzer explizit gefordert): `pg_dump` auf einen Zeitplan
  legen (z. B. täglich), Dump-Dateien an einem Ort außerhalb des Docker-Volumes ablegen (eigener
  Ordner im Projekt oder außerhalb, je nachdem wie der Nutzer das später einrichten möchte)
- **Restore-Weg**: `pg_restore`/`psql < dump.sql` gegen die laufende Datenbank — sollte einmal
  durchgetestet werden, sobald die Datenbank befüllt ist, damit im Ernstfall klar ist, dass es
  funktioniert, statt es im Ernstfall zum ersten Mal auszuprobieren

### Konkrete Umsetzung (Stand: vom `database`-Agenten eingerichtet)

- **Backup-Skript**: `scripts/backup-db.ps1` — führt `pg_dump -F c` (Custom-Format, komprimiert,
  `pg_restore`-fähig) im laufenden `postgres`-Container aus, kopiert den Dump per `docker cp` nach
  `backups/` im Repo-Root (also **außerhalb** des benannten Docker-Volumes) und löscht ihn danach
  wieder aus dem Container. `backups/` ist per `.gitignore` (`/backups/`) vom Repo ausgeschlossen.
  Dateiname: `betquote-<env>-<Timestamp>.dump`.
- **Rotation**: 30 Tage Aufbewahrung (Default, per `-RetentionDays` änderbar) — ältere Dumps
  desselben Environments werden bei jedem Backup-Lauf automatisch gelöscht.
- **Automatisierung**: `scripts/register-backup-task.ps1` richtet einen **Windows Scheduled
  Task** (`BetQuote-DB-Backup`, täglich 03:30 Uhr) ein, der `backup-db.ps1 -Env dev` aufruft.
  Bewusst ein Windows Scheduled Task statt eines Cronjobs *im* Postgres-Container: der Stack läuft
  auf diesem Windows-Rechner (Docker Desktop), der Task Scheduler ist hier die native
  Host-seitige Entsprechung, ohne dass das Alpine-basierte Postgres-Image um einen
  Cron-Daemon erweitert werden müsste. Voraussetzung: der Dev-Stack muss zum Zeitpunkt des
  Backups laufen — `backup-db.ps1` prüft das selbst und bricht mit einer klaren Fehlermeldung ab,
  statt ein leeres/fehlerhaftes Backup zu erzeugen, falls der Container nicht läuft.
  Einmalig ausführen: `.\scripts\register-backup-task.ps1`. Task entfernen:
  `Unregister-ScheduledTask -TaskName "BetQuote-DB-Backup" -Confirm:$false`.
- **Restore-Skript**: `scripts/restore-db.ps1 -DumpFile <pfad-zum-dump>` — spielt den Dump per
  Default in eine **separate Test-Datenbank** (`betquote_restore_test`) auf demselben
  Postgres-Server ein, nicht ungefragt in die echte laufende Datenbank. Ein Restore direkt in die
  produktiv genutzte Datenbank (`-TargetDb betquote -Force`) überschreibt/löscht dort bestehende
  Daten und erfordert das explizite `-Force`-Flag.
- **Restore-Test durchgeführt** (2026-09-21, gegen den frisch aufgesetzten Dev-Stack): Dump
  erzeugt (`betquote-dev-2026-09-21_202151.dump`, 60,5 KB bei damals nur Schema + Seed-Daten,
  keine echten Spieldaten), in `betquote_restore_test` eingespielt, verifiziert (alle 18 Tabellen
  vorhanden, Seed-Daten der `datenquellen`-Tabelle mit allen 6 Zeilen korrekt wiederhergestellt),
  Test-Datenbank danach wieder gelöscht. Der komplette Weg — Dump → Kopie aus dem Container →
  Restore in eine frische DB → inhaltliche Verifikation — funktioniert nachweislich. Zusätzlich
  wurde der Scheduled Task einmal manuell angestoßen (`Start-ScheduledTask`) und hat erfolgreich
  einen weiteren Dump erzeugt (`LastTaskResult = 0`) — die komplette Automatisierungskette ist
  damit einmal end-to-end verifiziert, nicht nur die Einzelskripte für sich.
- **Offener Punkt / spätere Iteration**: der eingerichtete Scheduled Task sichert aktuell nur den
  **Dev**-Stack (`-Env dev`). Für den Prod/Live-Stack (`docker-compose.prod.yml`) ist noch kein
  eigener Task eingerichtet, da dieser laut `docker-compose.prod.yml` selbst noch nicht
  vollständig lauffähig ist (siehe Kommentar dort). Sobald der Prod-Stack real genutzt wird:
  `.\scripts\register-backup-task.ps1` um einen zweiten Task mit `-Env prod` ergänzen (Skript
  unterstützt `-Env` bereits, nur die Registrierung fehlt noch als zweiter Aufruf).

---

## Repository-Struktur (Stand: im Aufbau)

```
BetQuote/
├── backend/          # .NET-Backend (API, Datenimport, Vorhersagemodell)
├── frontend/          # React/Vite/TS-Frontend
├── docker/            # Docker-Compose-Konfiguration, Caddyfile
├── scripts/            # start.ps1, Hilfsskripte
├── PREDICTION_FACTORS.md   # Fachliche Grundlage: Einflussfaktoren, Datenquellen, Modellansatz
├── APP_STRUCTURE.md        # Seitenaufbau/UI-Struktur
└── TECH_STACK.md           # Diese Datei
```
