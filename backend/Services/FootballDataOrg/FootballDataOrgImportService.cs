using BetQuote.Api.Data;
using BetQuote.Api.Data.Entities;
using BetQuote.Api.Services.Common;
using BetQuote.Api.Services.Elo;
using BetQuote.Api.Services.OpenLigaDb;
using Microsoft.EntityFrameworkCore;

namespace BetQuote.Api.Services.FootballDataOrg;

public interface IFootballDataOrgImportService
{
    /// <summary>Voller Import: alle Wettbewerbe mit FootballDataOrgCode (aktuell nur La Liga), alle Saisons seit dem konfigurierten Startjahr.</summary>
    Task<ImportSummary> ImportAllAsync(CancellationToken ct = default);

    /// <summary>Import nur der aktuell laufenden Spiele - fuer haeufigeres Live-Polling.</summary>
    Task<ImportSummary> ImportCurrentAsync(CancellationToken ct = default);
}

/// <summary>
/// Importiert Spiele/Ergebnisse von football-data.org fuer Wettbewerbe, die NICHT ueber OpenLigaDB
/// laufen (aktuell nur La Liga, siehe PREDICTION_FACTORS.md "Ziel-Wettbewerbe" - Scope-Aenderung).
/// Strukturell an <see cref="OpenLigaDbImportService"/> angelehnt (gleiches Upsert-Prinzip per
/// externer Match-Id, gleiches Team-Alias-Mapping, gleiches Pro-Saison-Scope-Pattern gegen
/// ChangeTracker-Aufblaehung - siehe dortige Klassendoku fuer die Begruendung), aber mit
/// zusaetzlicher Ruecksicht auf das deutlich engere Rate-Limit (10 Requests/Minute im kostenlosen
/// Tier, siehe FootballDataOrgClient) - Saisons werden daher sequenziell mit einer kleinen Pause
/// zwischen den Requests abgerufen statt parallel/ungebremst.
///
/// OFFENER PUNKT: ohne konfigurierten FOOTBALL_DATA_ORG_API_KEY (siehe FootballDataOrgClient)
/// liefert dieser Service sofort eine leere ImportSummary mit einer erklaerenden Fehlermeldung,
/// statt zu werfen - der Rest der App (inkl. OpenLigaDB-Import fuer bl1/bl2) bleibt davon
/// unbeeintraechtigt.
/// </summary>
public class FootballDataOrgImportService(
    IFootballDataOrgClient client,
    BetQuoteDbContext db,
    IEloRatingService eloService,
    IServiceScopeFactory scopeFactory,
    ILogger<FootballDataOrgImportService> logger) : IFootballDataOrgImportService
{
    /// <summary>Kleine Pause zwischen Saison-Requests, um das 10-Req/Min-Limit (football-data.org Free-Tier) nicht zu reissen - mehrere Requests pro Saison moeglich (Score-Endpoint ist aber bereits die komplette Saison in einem Call, daher reicht diese Pause zwischen Saisons).</summary>
    private static readonly TimeSpan PauseZwischenSaisons = TimeSpan.FromSeconds(7);

    public async Task<ImportSummary> ImportAllAsync(CancellationToken ct = default)
    {
        if (!client.IstKonfiguriert)
        {
            logger.LogWarning("football-data.org-Import uebersprungen: FOOTBALL_DATA_ORG_API_KEY ist nicht gesetzt - siehe QUELLEN.md/Abschlussbericht fuer die Registrierung (https://www.football-data.org/client/register)");
            return ImportSummary.Empty with { Fehler = ["FOOTBALL_DATA_ORG_API_KEY nicht konfiguriert - La-Liga-Import uebersprungen"] };
        }

        var summary = ImportSummary.Empty;
        var wettbewerbe = await db.Wettbewerbe.AsNoTracking()
            .Where(w => w.FootballDataOrgCode != null)
            .ToListAsync(ct);

        foreach (var wettbewerb in wettbewerbe)
        {
            // football-data.org liefert im kostenlosen Tier typischerweise nur wenige zurueckliegende
            // Saisons (Doku nennt keine feste Zahl, praktisch meist ~5-6 Jahre) - wir versuchen ab
            // einem grosszuegigen Startjahr und lassen einzelne leere/fehlerhafte Saisons einfach als
            // Fehler durchlaufen (Aufgabenstellung: robust statt Abbruch), statt die exakte Grenze
            // hart zu kodieren.
            var minSaison = 2015;
            var aktuellesJahr = DateTime.UtcNow.Year;

            for (var saisonJahr = minSaison; saisonJahr <= aktuellesJahr; saisonJahr++)
            {
                try
                {
                    using var saisonScope = scopeFactory.CreateScope();
                    var saisonDb = saisonScope.ServiceProvider.GetRequiredService<BetQuoteDbContext>();
                    var saisonEloService = saisonScope.ServiceProvider.GetRequiredService<IEloRatingService>();

                    var teilSummary = await ImportSaisonAsync(saisonDb, saisonEloService, wettbewerb, saisonJahr, ct);
                    summary = summary.Add(teilSummary);
                }
                catch (Exception ex)
                {
                    var fehlerText = $"{wettbewerb.FootballDataOrgCode}/{saisonJahr}: {ex.Message}";
                    logger.LogWarning(ex, "football-data.org-Import fehlgeschlagen fuer {Wettbewerb} Saison {Saison} - Rest laeuft weiter", wettbewerb.Name, saisonJahr);
                    summary = summary with { Fehler = [.. summary.Fehler, fehlerText] };
                }

                try
                {
                    await Task.Delay(PauseZwischenSaisons, ct);
                }
                catch (TaskCanceledException)
                {
                    return summary;
                }
            }
        }

        return summary;
    }

    public async Task<ImportSummary> ImportCurrentAsync(CancellationToken ct = default)
    {
        if (!client.IstKonfiguriert)
        {
            return ImportSummary.Empty;
        }

        var summary = ImportSummary.Empty;
        var wettbewerbe = await db.Wettbewerbe.AsNoTracking()
            .Where(w => w.FootballDataOrgCode != null)
            .ToListAsync(ct);

        foreach (var wettbewerb in wettbewerbe)
        {
            try
            {
                var matches = await client.GetCurrentMatchesAsync(wettbewerb.FootballDataOrgCode!, ct);
                var teilSummary = await UpsertMatchesAsync(db, eloService, wettbewerb, matches, ct);
                summary = summary.Add(teilSummary);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "football-data.org Live-Polling fehlgeschlagen fuer {Wettbewerb}", wettbewerb.Name);
                summary = summary with { Fehler = [.. summary.Fehler, $"{wettbewerb.Name}: {ex.Message}"] };
            }
        }

        return summary;
    }

    private async Task<ImportSummary> ImportSaisonAsync(BetQuoteDbContext saisonDb, IEloRatingService saisonEloService, Wettbewerb wettbewerb, int saisonJahr, CancellationToken ct)
    {
        var matches = await client.GetMatchesForSeasonAsync(wettbewerb.FootballDataOrgCode!, saisonJahr, ct);
        if (matches.Count == 0)
        {
            return ImportSummary.Empty;
        }

        await EnsureSaisonAsync(saisonDb, wettbewerb, saisonJahr, ct);

        return await UpsertMatchesAsync(saisonDb, saisonEloService, wettbewerb, matches, ct);
    }

    private static async Task<Saison> EnsureSaisonAsync(BetQuoteDbContext db, Wettbewerb wettbewerb, int saisonJahr, CancellationToken ct)
    {
        var saison = await db.Saisons.FirstOrDefaultAsync(
            s => s.WettbewerbId == wettbewerb.Id && s.Startjahr == saisonJahr, ct);

        if (saison is null)
        {
            var folgejahrKurz = (saisonJahr + 1) % 100;
            saison = new Saison
            {
                WettbewerbId = wettbewerb.Id,
                Name = $"{saisonJahr}/{folgejahrKurz:D2}",
                Startjahr = saisonJahr,
                IstAktuell = false,
            };
            db.Saisons.Add(saison);
            await db.SaveChangesAsync(ct);
        }

        return saison;
    }

    private async Task<ImportSummary> UpsertMatchesAsync(BetQuoteDbContext db, IEloRatingService eloService, Wettbewerb wettbewerb, List<FootballDataOrgMatchDto> matches, CancellationToken ct)
    {
        var spieleNeu = 0;
        var spieleAktualisiert = 0;
        var teamsNeu = 0;
        var fehler = new List<string>();

        var saisonsByJahr = await db.Saisons
            .Where(s => s.WettbewerbId == wettbewerb.Id)
            .ToDictionaryAsync(s => s.Startjahr, ct);

        foreach (var match in matches)
        {
            try
            {
                var saisonJahr = match.Season?.StartDate?.Year ?? match.UtcDate.Year;
                if (!saisonsByJahr.TryGetValue(saisonJahr, out var saison))
                {
                    saison = await EnsureSaisonAsync(db, wettbewerb, saisonJahr, ct);
                    saisonsByJahr[saisonJahr] = saison;
                }

                var (heimTeam, heimNeu) = await EnsureTeamAsync(db, match.HomeTeam, ct);
                var (auswaertsTeam, auswaertsNeu) = await EnsureTeamAsync(db, match.AwayTeam, ct);
                if (heimNeu) teamsNeu++;
                if (auswaertsNeu) teamsNeu++;

                var spiel = await db.Spiele.FirstOrDefaultAsync(s => s.FootballDataOrgMatchId == match.Id, ct);

                var istNeu = spiel is null;
                spiel ??= new Spiel
                {
                    FootballDataOrgMatchId = match.Id,
                    WettbewerbId = wettbewerb.Id,
                };

                ApplyMatchToSpiel(spiel, match, wettbewerb, saison, heimTeam, auswaertsTeam);

                if (istNeu)
                {
                    db.Spiele.Add(spiel);
                    await db.SaveChangesAsync(ct);
                    spieleNeu++;
                }
                else
                {
                    await db.SaveChangesAsync(ct);
                    spieleAktualisiert++;
                }

                // football-data.org liefert im kostenlosen Tier keine Torschuetzen-/Event-Liste im
                // Matches-Endpoint (das waere ein separater, kostenpflichtiger/eingeschraenkter
                // Endpoint) - TorEvents bleiben fuer La-Liga-Spiele daher bewusst leer. Betrifft
                // nur die Torschuetzenliste in der UI (APP_STRUCTURE.md Spiel-Detail), nicht das
                // Endergebnis selbst oder die Vorhersage-Berechnung.
                if (spiel.Status == SpielStatus.Beendet)
                {
                    var hatEloHistorie = await db.EloRatingHistorien.AnyAsync(e => e.SpielId == spiel.Id, ct);
                    if (!hatEloHistorie)
                    {
                        await eloService.UpdateRatingsForSpielAsync(spiel.Id, ct);
                    }
                }

                db.ChangeTracker.Clear();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Import von football-data.org-Match {MatchId} fehlgeschlagen, uebersprungen", match.Id);
                fehler.Add($"Match {match.Id}: {ex.Message}");
            }
        }

        return new ImportSummary(spieleNeu, spieleAktualisiert, teamsNeu, fehler);
    }

    private static void ApplyMatchToSpiel(Spiel spiel, FootballDataOrgMatchDto match, Wettbewerb wettbewerb, Saison saison, Team heimTeam, Team auswaertsTeam)
    {
        spiel.WettbewerbId = wettbewerb.Id;
        spiel.SaisonId = saison.Id;
        spiel.SpieltagNummer = match.Matchday ?? 0;
        spiel.SpieltagName = match.Matchday.HasValue ? $"{match.Matchday}. Spieltag" : null;
        spiel.HeimTeamId = heimTeam.Id;
        spiel.AuswaertsTeamId = auswaertsTeam.Id;
        spiel.Anstoss = match.UtcDate;

        var (kwJahr, kwNummer) = IsoWeekHelper.GetIsoYearAndWeek(match.UtcDate);
        spiel.KwJahr = kwJahr;
        spiel.KwNummer = kwNummer;

        spiel.HalbzeitToreHeim = match.Score.HalfTime.Home;
        spiel.HalbzeitToreAuswaerts = match.Score.HalfTime.Away;

        spiel.Status = match.Status switch
        {
            "FINISHED" => SpielStatus.Beendet,
            "IN_PLAY" or "PAUSED" => SpielStatus.Live,
            "POSTPONED" or "SUSPENDED" or "CANCELLED" => SpielStatus.Abgesagt,
            _ => SpielStatus.Geplant,
        };

        spiel.LiveMinute = spiel.Status == SpielStatus.Live ? match.Minute : null;

        if (spiel.Status is SpielStatus.Beendet or SpielStatus.Live)
        {
            spiel.ToreHeim = match.Score.FullTime.Home;
            spiel.ToreAuswaerts = match.Score.FullTime.Away;
        }
        else
        {
            spiel.ToreHeim = null;
            spiel.ToreAuswaerts = null;
        }
    }

    /// <summary>Team-Mapping analog zu OpenLigaDbImportService.EnsureTeamAsync, aber ueber die Datenquelle "footballdataorg" (eigener Alias-Namensraum, da football-data.org teils andere Schreibweisen als OpenLigaDB nutzt - PREDICTION_FACTORS.md "Team-/Spiel-Mapping").</summary>
    private static async Task<(Team Team, bool WurdeNeuAngelegt)> EnsureTeamAsync(BetQuoteDbContext db, FootballDataOrgTeamDto dto, CancellationToken ct)
    {
        const string datenquelleSchluessel = "footballdataorg";

        var alias = await db.TeamAliase
            .Include(a => a.Team)
            .FirstOrDefaultAsync(a => a.Datenquelle.Schluessel == datenquelleSchluessel && a.AliasName == dto.Name, ct);

        if (alias is not null)
        {
            if (!string.IsNullOrWhiteSpace(dto.Crest) && alias.Team.LogoUrl != dto.Crest)
            {
                alias.Team.LogoUrl = dto.Crest;
                await db.SaveChangesAsync(ct);
            }
            return (alias.Team, false);
        }

        var existierendesTeam = await db.Teams.FirstOrDefaultAsync(t => t.Name == dto.Name, ct);
        if (existierendesTeam is not null)
        {
            await AddAliasAsync(db, existierendesTeam.Id, dto.Name, ct);
            return (existierendesTeam, false);
        }

        var neuesTeam = new Team
        {
            Name = dto.Name,
            Kurzname = dto.ShortName,
            LogoUrl = dto.Crest,
        };
        db.Teams.Add(neuesTeam);
        await db.SaveChangesAsync(ct);

        await AddAliasAsync(db, neuesTeam.Id, dto.Name, ct);
        return (neuesTeam, true);
    }

    private static async Task AddAliasAsync(BetQuoteDbContext db, int teamId, string aliasName, CancellationToken ct)
    {
        var datenquelle = await db.Datenquellen.AsNoTracking().FirstAsync(d => d.Schluessel == "footballdataorg", ct);

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
