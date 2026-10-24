using BetQuote.Api.Data;
using BetQuote.Api.Data.Entities;
using BetQuote.Api.Services.Common;
using BetQuote.Api.Services.Elo;
using Microsoft.EntityFrameworkCore;

namespace BetQuote.Api.Services.OpenLigaDb;

public interface IOpenLigaDbImportService
{
    /// <summary>Voller Import: alle 4 Wettbewerbe, alle verfuegbaren Saisons. Fuer den initialen Befuellungslauf und periodische Vollabgleiche.</summary>
    Task<ImportSummary> ImportAllAsync(CancellationToken ct = default);

    /// <summary>Import nur der laufenden/aktuellen Saison je Wettbewerb - fuer haeufiges Live-Polling, deutlich billiger als ein Vollimport.</summary>
    Task<ImportSummary> ImportCurrentSeasonsAsync(CancellationToken ct = default);
}

public record ImportSummary(int SpieleNeu, int SpieleAktualisiert, int TeamsNeu, List<string> Fehler)
{
    public static ImportSummary Empty => new(0, 0, 0, []);

    public ImportSummary Add(ImportSummary other) => new(
        SpieleNeu + other.SpieleNeu,
        SpieleAktualisiert + other.SpieleAktualisiert,
        TeamsNeu + other.TeamsNeu,
        [.. Fehler, .. other.Fehler]);
}

/// <summary>
/// Importiert Spiele/Ergebnisse/Torereignisse von OpenLigaDB fuer alle vier Zielwettbewerbe
/// (siehe PREDICTION_FACTORS.md "Ziel-Wettbewerbe": bl1, bl2, bl3, dfb). Wird wiederholt aufgerufen
/// (Scheduling siehe <see cref="OpenLigaDbPollingBackgroundService"/>), daher komplett als
/// Upsert angelegt: existierende Spiele (per OpenLigaDbMatchId) werden aktualisiert statt dupliziert.
///
/// Fehlerhandling (Aufgabenstellung: "ein einzelner fehlgeschlagener Request darf nicht den ganzen
/// Importlauf abbrechen"): jede Liga/Saison-Kombination laeuft in einem eigenen try/catch - ein
/// Fehler (z.B. OpenLigaDB gerade nicht erreichbar fuer eine bestimmte Saison) wird gesammelt und
/// am Ende im ImportSummary zurueckgegeben, der Rest des Imports laeuft trotzdem weiter.
///
/// WICHTIG (Performance/Stabilitaet): <see cref="ImportAllAsync"/> arbeitet ueber ~25 Saisons x 4
/// Wettbewerbe mit insgesamt zehntausenden Spielen/Torereignissen. Wuerde das komplett ueber einen
/// einzigen langlebigen <see cref="BetQuoteDbContext"/> laufen, waechst dessen ChangeTracker ueber
/// den gesamten Lauf hinweg ungebremst (jedes geladene/veraenderte Spiel/Team/TorEvent bleibt bis
/// zum Ende des Scopes getrackt) - DetectChanges bei jedem SaveChanges wird dadurch zunehmend
/// teurer (beobachtet: der Prozess lief nach einigen tausend Spielen bei nahezu 100% CPU fest, ohne
/// neue Zeilen mehr zu schreiben). Deshalb erzeugt <see cref="ImportAllAsync"/> ueber die
/// injizierte <see cref="IServiceScopeFactory"/> PRO SAISON einen frischen Scope/DbContext, der
/// danach sofort wieder verworfen wird. Alle privaten Hilfsmethoden nehmen den DbContext/
/// EloRatingService deshalb explizit als Parameter entgegen statt ein Instanzfeld zu nutzen - der
/// injizierte <c>db</c>/<c>eloService</c> wird nur noch fuer den kleineren, unproblematischen
/// Aufrufpfad <see cref="ImportCurrentSeasonsAsync"/> (max. ein Spieltag pro Wettbewerb) genutzt.
/// </summary>
public class OpenLigaDbImportService(
    IOpenLigaDbClient client,
    BetQuoteDbContext db,
    IEloRatingService eloService,
    IServiceScopeFactory scopeFactory,
    ILogger<OpenLigaDbImportService> logger) : IOpenLigaDbImportService
{
    // Frueheste bekannt verlaessliche Saison je Wettbewerb, siehe PREDICTION_FACTORS.md
    // "Wichtige Einschraenkung, die wir bewusst akzeptieren": bl1 ab ~2001 digital verlaesslich
    // (OpenLigaDB selbst fuehrt teils frueher, aber Datenqualitaet variiert - wir nehmen den von
    // OpenLigaDB tatsaechlich gemeldeten Bereich ueber getavailableleagues, s.u., statt hart zu
    // kodieren, und schneiden nur am unteren Ende auf ein plausibles Startjahr zu, um DFB-Pokal-
    // /3.-Liga-Vorlaeuferwettbewerbe mit anderer Struktur auszuschliessen).
    private static readonly Dictionary<string, int> FruehesteSaisonProWettbewerb = new()
    {
        ["bl1"] = 2001,
        ["bl2"] = 2006,
        ["bl3"] = 2008,
        ["dfb"] = 2007,
    };

    public async Task<ImportSummary> ImportAllAsync(CancellationToken ct = default)
    {
        var summary = ImportSummary.Empty;

        // Wettbewerbe einmalig ueber den injizierten (langlebigen) Context laden - reine
        // Stammdaten (4 Zeilen), kein ChangeTracker-Problem.
        var wettbewerbe = await db.Wettbewerbe.AsNoTracking().ToListAsync(ct);

        List<OpenLigaDbLeagueDto> verfuegbareLigen;
        try
        {
            verfuegbareLigen = await client.GetAvailableLeaguesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OpenLigaDB-Import abgebrochen: getavailableleagues nicht erreichbar");
            return summary with { Fehler = [$"getavailableleagues fehlgeschlagen: {ex.Message}"] };
        }

        foreach (var wettbewerb in wettbewerbe)
        {
            // Wettbewerbe ohne OpenLigaDbKuerzel (z.B. La Liga) laufen ueber eine andere Quelle
            // (siehe FootballDataOrgImportService) - hier einfach ueberspringen statt Fehler.
            if (string.IsNullOrWhiteSpace(wettbewerb.OpenLigaDbKuerzel))
            {
                continue;
            }

            var minSaison = FruehesteSaisonProWettbewerb.GetValueOrDefault(wettbewerb.OpenLigaDbKuerzel, 2001);

            var saisonJahre = verfuegbareLigen
                .Where(l => l.LeagueShortcut == wettbewerb.OpenLigaDbKuerzel)
                .Select(l => ParseSaisonJahr(l.LeagueSeason))
                .Where(j => j.HasValue && j.Value >= minSaison)
                .Select(j => j!.Value)
                .Distinct()
                .OrderBy(j => j)
                .ToList();

            foreach (var saisonJahr in saisonJahre)
            {
                try
                {
                    // Frischer Scope/DbContext PRO SAISON (siehe Klassendoku) - haelt den
                    // ChangeTracker klein und damit SaveChanges/DetectChanges konstant schnell,
                    // unabhaengig davon wie viele Saisons/Spiele der Gesamtlauf schon verarbeitet hat.
                    using var saisonScope = scopeFactory.CreateScope();
                    var saisonDb = saisonScope.ServiceProvider.GetRequiredService<BetQuoteDbContext>();
                    var saisonEloService = saisonScope.ServiceProvider.GetRequiredService<IEloRatingService>();

                    var teilSummary = await ImportSaisonAsync(saisonDb, saisonEloService, wettbewerb, saisonJahr, ct);
                    summary = summary.Add(teilSummary);
                }
                catch (Exception ex)
                {
                    var fehlerText = $"{wettbewerb.OpenLigaDbKuerzel}/{saisonJahr}: {ex.Message}";
                    logger.LogWarning(ex, "Import fehlgeschlagen fuer {Liga} Saison {Saison} - Rest des Imports laeuft weiter", wettbewerb.OpenLigaDbKuerzel, saisonJahr);
                    summary = summary with { Fehler = [.. summary.Fehler, fehlerText] };
                }
            }
        }

        return summary;
    }

    public async Task<ImportSummary> ImportCurrentSeasonsAsync(CancellationToken ct = default)
    {
        var summary = ImportSummary.Empty;
        var wettbewerbe = await db.Wettbewerbe.AsNoTracking().ToListAsync(ct);

        foreach (var wettbewerb in wettbewerbe)
        {
            if (string.IsNullOrWhiteSpace(wettbewerb.OpenLigaDbKuerzel))
            {
                continue;
            }

            try
            {
                var matches = await client.GetCurrentMatchdayMatchesAsync(wettbewerb.OpenLigaDbKuerzel, ct);
                var teilSummary = await UpsertMatchesAsync(db, eloService, wettbewerb, matches, ct);
                summary = summary.Add(teilSummary);
            }
            catch (Exception ex)
            {
                var fehlerText = $"{wettbewerb.OpenLigaDbKuerzel} (aktuell): {ex.Message}";
                logger.LogWarning(ex, "Live-Polling-Import fehlgeschlagen fuer {Liga}", wettbewerb.OpenLigaDbKuerzel);
                summary = summary with { Fehler = [.. summary.Fehler, fehlerText] };
            }
        }

        return summary;
    }

    private async Task<ImportSummary> ImportSaisonAsync(BetQuoteDbContext saisonDb, IEloRatingService saisonEloService, Wettbewerb wettbewerb, int saisonJahr, CancellationToken ct)
    {
        // Aufrufer (ImportAllAsync) hat bereits auf null geprueft und ueberspringt Wettbewerbe
        // ohne OpenLigaDbKuerzel - hier daher sicher nicht null.
        var matches = await client.GetMatchesForSeasonAsync(wettbewerb.OpenLigaDbKuerzel!, saisonJahr.ToString(), ct);
        if (matches.Count == 0)
        {
            return ImportSummary.Empty;
        }

        await EnsureSaisonAsync(saisonDb, wettbewerb, saisonJahr, ct);

        return await UpsertMatchesAsync(saisonDb, saisonEloService, wettbewerb, matches, ct);
    }

    private static async Task<Saison> EnsureSaisonAsync(BetQuoteDbContext db, Wettbewerb wettbewerb, int saisonJahr, CancellationToken ct)
    {
        var name = FormatSaisonName(saisonJahr);

        var saison = await db.Saisons.FirstOrDefaultAsync(
            s => s.WettbewerbId == wettbewerb.Id && s.Startjahr == saisonJahr, ct);

        if (saison is null)
        {
            saison = new Saison
            {
                WettbewerbId = wettbewerb.Id,
                Name = name,
                Startjahr = saisonJahr,
                OpenLigaDbSaisonKuerzel = saisonJahr.ToString(),
                IstAktuell = false,
            };
            db.Saisons.Add(saison);
            await db.SaveChangesAsync(ct);
        }

        return saison;
    }

    private static string FormatSaisonName(int startjahr)
    {
        // 3. Liga/DFB-Pokal laufen wie 1./2. Liga im "Sommer bis Sommer"-Rhythmus (Saison
        // 2025/26 startet im Jahr 2025) - einheitliches Anzeigeformat ueber alle 4 Wettbewerbe.
        var folgejahrKurz = (startjahr + 1) % 100;
        return $"{startjahr}/{folgejahrKurz:D2}";
    }

    private static int? ParseSaisonJahr(string? leagueSeason)
    {
        return int.TryParse(leagueSeason, out var jahr) ? jahr : null;
    }

    /// <summary>
    /// Kernstueck des Imports: fuer jedes gelieferte Match pruefen ob es (per OpenLigaDbMatchId)
    /// schon existiert, sonst anlegen; Teams werden dabei per Alias aufgeloest/neu angelegt
    /// (siehe TeamAlias-Dokumentation). Nach jedem tatsaechlich abgeschlossenen Spiel wird die
    /// Elo-Neuberechnung angestossen (PREDICTION_FACTORS.md Abschnitt 1).
    /// </summary>
    private async Task<ImportSummary> UpsertMatchesAsync(BetQuoteDbContext db, IEloRatingService eloService, Wettbewerb wettbewerb, List<OpenLigaDbMatchDto> matches, CancellationToken ct)
    {
        var spieleNeu = 0;
        var spieleAktualisiert = 0;
        var teamsNeu = 0;
        var fehler = new List<string>();

        // Saisons dieses Wettbewerbs vorab laden (Cache fuer die Zuordnung Match -> Saison-Entity).
        var saisonsByJahr = await db.Saisons
            .Where(s => s.WettbewerbId == wettbewerb.Id)
            .ToDictionaryAsync(s => s.Startjahr, ct);

        foreach (var match in matches)
        {
            try
            {
                if (match.LeagueSeason.HasValue && !saisonsByJahr.ContainsKey(match.LeagueSeason.Value))
                {
                    var neueSaison = await EnsureSaisonAsync(db, wettbewerb, match.LeagueSeason.Value, ct);
                    saisonsByJahr[match.LeagueSeason.Value] = neueSaison;
                }

                var saisonJahr = match.LeagueSeason ?? saisonsByJahr.Keys.OrderByDescending(j => j).FirstOrDefault();
                if (!saisonsByJahr.TryGetValue(saisonJahr, out var saison))
                {
                    fehler.Add($"Match {match.MatchId}: keine Saison ermittelbar, uebersprungen");
                    continue;
                }

                var (heimTeam, heimNeu) = await EnsureTeamAsync(db, match.Team1, ct);
                var (auswaertsTeam, auswaertsNeu) = await EnsureTeamAsync(db, match.Team2, ct);
                if (heimNeu) teamsNeu++;
                if (auswaertsNeu) teamsNeu++;

                var spiel = await db.Spiele
                    .Include(s => s.TorEvents)
                    .FirstOrDefaultAsync(s => s.OpenLigaDbMatchId == match.MatchId, ct);

                var istNeu = spiel is null;
                spiel ??= new Spiel
                {
                    OpenLigaDbMatchId = match.MatchId,
                    WettbewerbId = wettbewerb.Id,
                };

                ApplyMatchToSpiel(spiel, match, wettbewerb, saison, heimTeam, auswaertsTeam);

                if (istNeu)
                {
                    db.Spiele.Add(spiel);
                    await db.SaveChangesAsync(ct); // Id wird fuer TorEvents/Elo gebraucht.
                    spieleNeu++;
                }
                else
                {
                    await db.SaveChangesAsync(ct);
                    spieleAktualisiert++;
                }

                await SyncTorEventsAsync(db, spiel, match, heimTeam, auswaertsTeam, ct);

                // Elo nur neu berechnen, wenn das Spiel tatsaechlich beendet ist UND noch keine
                // Elo-Historie dafuer existiert (verhindert doppelte Elo-Updates bei wiederholten
                // Live-Polling-Laeufen desselben bereits abgeschlossenen Spiels).
                if (spiel.Status == SpielStatus.Beendet)
                {
                    var hatEloHistorie = await db.EloRatingHistorien.AnyAsync(e => e.SpielId == spiel.Id, ct);
                    if (!hatEloHistorie)
                    {
                        await eloService.UpdateRatingsForSpielAsync(spiel.Id, ct);
                    }
                }

                // ChangeTracker regelmaessig leeren: selbst mit dem Pro-Saison-Scope (siehe
                // Klassendoku) kann eine einzelne Saison mehrere hundert Spiele umfassen - ohne
                // Clear() wuerden auch innerhalb einer Saison zunehmend mehr Entities getrackt
                // bleiben. AsNoTracking() ist hier nicht durchgaengig nutzbar, da wir Entities
                // (Team/Spiel/TorEvents) gezielt aendern/anlegen muessen.
                db.ChangeTracker.Clear();
            }
            catch (Exception ex)
            {
                // Einzelnes Match darf den Rest des Imports nicht stoppen (Aufgabenstellung).
                logger.LogWarning(ex, "Import von Match {MatchId} fehlgeschlagen, uebersprungen", match.MatchId);
                fehler.Add($"Match {match.MatchId}: {ex.Message}");
            }
        }

        return new ImportSummary(spieleNeu, spieleAktualisiert, teamsNeu, fehler);
    }

    private static void ApplyMatchToSpiel(Spiel spiel, OpenLigaDbMatchDto match, Wettbewerb wettbewerb, Saison saison, Team heimTeam, Team auswaertsTeam)
    {
        spiel.WettbewerbId = wettbewerb.Id;
        spiel.SaisonId = saison.Id;
        spiel.SpieltagNummer = match.Group?.GroupOrderId ?? 0;
        spiel.SpieltagName = match.Group?.GroupName;
        spiel.HeimTeamId = heimTeam.Id;
        spiel.AuswaertsTeamId = auswaertsTeam.Id;

        var anstoss = match.MatchDateTimeUtc ?? spiel.Anstoss;
        spiel.Anstoss = anstoss;

        var (kwJahr, kwNummer) = IsoWeekHelper.GetIsoYearAndWeek(anstoss);
        spiel.KwJahr = kwJahr;
        spiel.KwNummer = kwNummer;

        var endergebnis = match.MatchResults.FirstOrDefault(r => r.ResultTypeKind == "After90Minutes")
            ?? match.MatchResults.LastOrDefault();
        var halbzeit = match.MatchResults.FirstOrDefault(r => r.ResultTypeKind == "HalfTime");

        if (halbzeit is not null)
        {
            spiel.HalbzeitToreHeim = halbzeit.PointsTeam1;
            spiel.HalbzeitToreAuswaerts = halbzeit.PointsTeam2;
        }

        if (match.MatchIsFinished)
        {
            spiel.Status = SpielStatus.Beendet;
            spiel.LiveMinute = null;
            if (endergebnis is not null)
            {
                spiel.ToreHeim = endergebnis.PointsTeam1;
                spiel.ToreAuswaerts = endergebnis.PointsTeam2;
            }
        }
        else if (endergebnis is not null && (endergebnis.PointsTeam1 > 0 || endergebnis.PointsTeam2 > 0 || match.Goals.Count > 0)
                 && anstoss <= DateTimeOffset.UtcNow)
        {
            // Noch nicht als beendet markiert, aber es liegt bereits ein Zwischenstand vor und der
            // Anstoss liegt in der Vergangenheit -> Spiel laeuft gerade.
            spiel.Status = SpielStatus.Live;
            spiel.ToreHeim = endergebnis.PointsTeam1;
            spiel.ToreAuswaerts = endergebnis.PointsTeam2;
            spiel.LiveMinute = match.Goals.Count > 0 ? match.Goals.Max(g => g.MatchMinute ?? 0) : null;
        }
        else if (anstoss <= DateTimeOffset.UtcNow && !match.MatchIsFinished)
        {
            // Anstoss liegt in der Vergangenheit, aber (noch) kein Ergebnis/Tor gemeldet -
            // wahrscheinlich gerade angepfiffen oder OpenLigaDB hinkt der Live-Meldung leicht
            // hinterher. Als Live fuehren statt Geplant, damit es in der UI korrekt als laufend
            // markiert wird; Status springt beim naechsten Poll automatisch auf Beendet um.
            spiel.Status = SpielStatus.Live;
        }
        else
        {
            spiel.Status = SpielStatus.Geplant;
            spiel.ToreHeim = null;
            spiel.ToreAuswaerts = null;
        }
    }

    private static async Task SyncTorEventsAsync(BetQuoteDbContext db, Spiel spiel, OpenLigaDbMatchDto match, Team heimTeam, Team auswaertsTeam, CancellationToken ct)
    {
        var vorhandene = await db.TorEvents.Where(t => t.SpielId == spiel.Id).ToListAsync(ct);

        // OpenLigaDB liefert keine stabile Tor-Id, die wir direkt referenzieren wuerden (goalID
        // ist vorhanden, aber wir bilden das Modell bewusst schlank ohne diese Fremd-Id ab) -
        // einfachste robuste Strategie: bei jedem Sync alle vorhandenen TorEvents des Spiels
        // ersetzen. Unproblematisch, da TorEvents ausschliesslich aus OpenLigaDB stammen (keine
        // manuellen Ergaenzungen, die hier verloren gehen koennten) und die Menge pro Spiel klein ist.
        if (vorhandene.Count > 0)
        {
            db.TorEvents.RemoveRange(vorhandene);
        }

        foreach (var goal in match.Goals)
        {
            var team = goal.ScoringTeamId == heimTeam.OpenLigaDbTeamId ? heimTeam
                : goal.ScoringTeamId == auswaertsTeam.OpenLigaDbTeamId ? auswaertsTeam
                : null;

            // Fallback falls ScoringTeamId nicht zugeordnet werden kann (sollte praktisch nicht
            // vorkommen, aber Aufgabenstellung fordert robustes Fehlerhandling statt Abbruch).
            team ??= heimTeam;

            db.TorEvents.Add(new TorEvent
            {
                SpielId = spiel.Id,
                TeamId = team.Id,
                SpielerNameFreitext = goal.GoalGetterName,
                Minute = goal.MatchMinute ?? 0,
                IstElfmeter = goal.IsPenalty,
                IstEigentor = goal.IsOwnGoal,
                // Fallback 0 falls OpenLigaDB ausnahmsweise null liefert (siehe DTO-Kommentar) -
                // besser ein plausibler Platzhalter als der ganze Match-Import wegen eines
                // einzelnen unvollstaendigen Tor-Datensatzes.
                ToreHeimNachEvent = goal.ScoreTeam1 ?? 0,
                ToreAuswaertsNachEvent = goal.ScoreTeam2 ?? 0,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Team-Mapping ueber TeamAlias (Datenquelle "openligadb"): zuerst per Alias suchen, dann per
    /// OpenLigaDbTeamId (falls das Team schon anderweitig angelegt wurde), sonst neu anlegen +
    /// Alias speichern - siehe PREDICTION_FACTORS.md "Team-/Spiel-Mapping".
    /// </summary>
    private static async Task<(Team Team, bool WurdeNeuAngelegt)> EnsureTeamAsync(BetQuoteDbContext db, OpenLigaDbTeamDto dto, CancellationToken ct)
    {
        const string datenquelleSchluessel = "openligadb";

        var alias = await db.TeamAliase
            .Include(a => a.Team)
            .FirstOrDefaultAsync(a => a.Datenquelle.Schluessel == datenquelleSchluessel && a.AliasName == dto.TeamName, ct);

        if (alias is not null)
        {
            // Logo-URL kann sich aendern (Wikimedia-Links etc.) - bei jedem Import auffrischen.
            if (!string.IsNullOrWhiteSpace(dto.TeamIconUrl) && alias.Team.LogoUrl != dto.TeamIconUrl)
            {
                alias.Team.LogoUrl = dto.TeamIconUrl;
                await db.SaveChangesAsync(ct);
            }
            return (alias.Team, false);
        }

        var existierendesTeam = await db.Teams.FirstOrDefaultAsync(t => t.OpenLigaDbTeamId == dto.TeamId, ct);
        if (existierendesTeam is not null)
        {
            await AddAliasAsync(db, existierendesTeam.Id, dto.TeamName, ct);
            return (existierendesTeam, false);
        }

        // Auch per exaktem Namen pruefen (z.B. bereits durch einen anderen Import-Pfad angelegt).
        existierendesTeam = await db.Teams.FirstOrDefaultAsync(t => t.Name == dto.TeamName, ct);
        if (existierendesTeam is not null)
        {
            existierendesTeam.OpenLigaDbTeamId ??= dto.TeamId;
            await db.SaveChangesAsync(ct);
            await AddAliasAsync(db, existierendesTeam.Id, dto.TeamName, ct);
            return (existierendesTeam, false);
        }

        var neuesTeam = new Team
        {
            Name = dto.TeamName,
            Kurzname = dto.ShortName,
            LogoUrl = dto.TeamIconUrl,
            OpenLigaDbTeamId = dto.TeamId,
        };
        db.Teams.Add(neuesTeam);
        await db.SaveChangesAsync(ct);

        await AddAliasAsync(db, neuesTeam.Id, dto.TeamName, ct);
        return (neuesTeam, true);
    }

    private static async Task AddAliasAsync(BetQuoteDbContext db, int teamId, string aliasName, CancellationToken ct)
    {
        var datenquelle = await db.Datenquellen.AsNoTracking().FirstAsync(d => d.Schluessel == "openligadb", ct);

        var existiert = await db.TeamAliase.AnyAsync(
            a => a.DatenquelleId == datenquelle.Id && a.AliasName == aliasName, ct);
        if (existiert)
        {
            return;
        }

        db.TeamAliase.Add(new TeamAlias
        {
            TeamId = teamId,
            DatenquelleId = datenquelle.Id,
            AliasName = aliasName,
        });
        await db.SaveChangesAsync(ct);
    }
}
