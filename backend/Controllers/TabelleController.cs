using BetQuote.Api.Controllers.Dtos;
using BetQuote.Api.Data;
using BetQuote.Api.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BetQuote.Api.Controllers;

/// <summary>Live-Tabelle pro Wettbewerb (APP_STRUCTURE.md Abschnitt 1b) - DFB-Pokal als Rundenuebersicht statt Tabelle, da kein Ligamodus.</summary>
[ApiController]
[Route("api/[controller]")]
public class TabelleController(BetQuoteDbContext db) : ControllerBase
{
    /// <summary>GET /api/tabelle/{wettbewerbId} - klassische Tabelle (Platz/Team/Spiele/Tordifferenz/Punkte) der aktuellen Saison. Fuer den DFB-Pokal: siehe /api/tabelle/{wettbewerbId}/pokal.</summary>
    [HttpGet("{wettbewerbId:int}")]
    public async Task<ActionResult<LigaTabelleDto>> GetTabelle(int wettbewerbId, [FromQuery] int? saisonId, CancellationToken ct)
    {
        var wettbewerb = await db.Wettbewerbe.AsNoTracking().FirstOrDefaultAsync(w => w.Id == wettbewerbId, ct);
        if (wettbewerb is null)
        {
            return NotFound(new { error = $"Wettbewerb mit Id {wettbewerbId} nicht gefunden." });
        }

        if (wettbewerb.IstPokal)
        {
            return BadRequest(new { error = "Fuer Pokal-Wettbewerbe gibt es keine Tabelle - siehe /api/tabelle/{wettbewerbId}/pokal." });
        }

        var saison = await ResolveSaisonAsync(wettbewerbId, saisonId, ct);
        if (saison is null)
        {
            return Ok(new LigaTabelleDto(wettbewerb.Id, wettbewerb.Name, wettbewerb.IstPokal, []));
        }

        var spiele = await db.Spiele
            .AsNoTracking()
            .Include(s => s.HeimTeam)
            .Include(s => s.AuswaertsTeam)
            .Where(s => s.SaisonId == saison.Id && s.Status == SpielStatus.Beendet && s.ToreHeim != null && s.ToreAuswaerts != null)
            .ToListAsync(ct);

        var titelverteidigerTeamId = await ErmittleTitelverteidigerAsync(wettbewerbId, saison, ct);
        var pokalsiegerTeamId = await ErmittlePokalsiegerAsync(wettbewerbId, ct);
        var aufsteigerTeamIds = await ErmittleAufsteigerAsync(wettbewerbId, saison, ct);

        var tabelle = BuildTabelle(spiele, titelverteidigerTeamId, pokalsiegerTeamId, aufsteigerTeamIds);

        return Ok(new LigaTabelleDto(wettbewerb.Id, wettbewerb.Name, wettbewerb.IstPokal, tabelle));
    }

    /// <summary>OpenLigaDB-Wettbewerb-Id der jeweils naechstniedrigeren deutschen Liga, fuer die Aufsteiger-Ermittlung (APP_STRUCTURE.md Abschnitt 1b "A fuer Aufsteiger"). 1. Bundesliga (Id 1) -&gt; 2. Bundesliga (Id 2) -&gt; 3. Liga (Id 3, nicht mehr aktiv importiert, aber historische Daten der letzten Saison noch vorhanden und ausreichend fuer diese Ermittlung). La Liga (Id 5) hat bewusst keinen Eintrag - Segunda Division nicht im Scope.</summary>
    private static readonly Dictionary<int, int> NiedrigereLigaProWettbewerb = new()
    {
        [1] = 2,
        [2] = 3,
    };

    /// <summary>
    /// Team-Ids, die in der uebergebenen (aktuellen) Saison erstmals in diesem Wettbewerb spielen,
    /// aber in der Vorsaison des naechstniedrigeren Wettbewerbs vertreten waren - "A"-Icon
    /// (APP_STRUCTURE.md Abschnitt 1b). Liefert eine leere Menge fuer Wettbewerbe ohne bekannte
    /// niedrigere Liga (z.B. La Liga) oder wenn keine Vorsaison-Daten vorliegen.
    /// </summary>
    private async Task<HashSet<int>> ErmittleAufsteigerAsync(int wettbewerbId, Saison aktuelleSaison, CancellationToken ct)
    {
        if (!NiedrigereLigaProWettbewerb.TryGetValue(wettbewerbId, out var niedrigereLigaId))
        {
            return [];
        }

        // Teams, die VOR der aktuellen Saison in diesem Wettbewerb (derselben Liga) gespielt haben -
        // alles andere in der aktuellen Saison, das dort NICHT vorkommt, kam von ausserhalb (Aufstieg
        // ODER Neuling ohne Tabellen-relevante Vorsaison in dieser Liga - fuer unsere Zwecke reicht
        // "kam nicht aus derselben Liga", da wir ohnehin nur gegen die niedrigere Liga abgleichen).
        var vorSaisonDerselbenLiga = await db.Saisons.AsNoTracking()
            .Where(s => s.WettbewerbId == wettbewerbId && s.Startjahr < aktuelleSaison.Startjahr)
            .OrderByDescending(s => s.Startjahr)
            .FirstOrDefaultAsync(ct);

        var teamsInAktuellerSaison = await db.Spiele.AsNoTracking()
            .Where(s => s.SaisonId == aktuelleSaison.Id)
            .Select(s => s.HeimTeamId)
            .Union(db.Spiele.AsNoTracking().Where(s => s.SaisonId == aktuelleSaison.Id).Select(s => s.AuswaertsTeamId))
            .ToListAsync(ct);

        var teamsInVorSaisonDerselbenLiga = vorSaisonDerselbenLiga is null
            ? []
            : await db.Spiele.AsNoTracking()
                .Where(s => s.SaisonId == vorSaisonDerselbenLiga.Id)
                .Select(s => s.HeimTeamId)
                .Union(db.Spiele.AsNoTracking().Where(s => s.SaisonId == vorSaisonDerselbenLiga.Id).Select(s => s.AuswaertsTeamId))
                .ToListAsync(ct);

        var neuInDieserLiga = teamsInAktuellerSaison.Except(teamsInVorSaisonDerselbenLiga).ToHashSet();
        if (neuInDieserLiga.Count == 0)
        {
            return [];
        }

        // Vorsaison der NIEDRIGEREN Liga ermitteln (gleiches Startjahr wie die Vorsaison dieser Liga,
        // da alle deutschen Ligen im selben Sommer-bis-Sommer-Rhythmus laufen).
        var vorSaisonStartjahr = aktuelleSaison.Startjahr - 1;
        var vorSaisonNiedrigereLiga = await db.Saisons.AsNoTracking()
            .FirstOrDefaultAsync(s => s.WettbewerbId == niedrigereLigaId && s.Startjahr == vorSaisonStartjahr, ct);

        if (vorSaisonNiedrigereLiga is null)
        {
            return [];
        }

        var teamsInVorSaisonNiedrigererLiga = await db.Spiele.AsNoTracking()
            .Where(s => s.SaisonId == vorSaisonNiedrigereLiga.Id)
            .Select(s => s.HeimTeamId)
            .Union(db.Spiele.AsNoTracking().Where(s => s.SaisonId == vorSaisonNiedrigereLiga.Id).Select(s => s.AuswaertsTeamId))
            .ToListAsync(ct);

        return neuInDieserLiga.Intersect(teamsInVorSaisonNiedrigererLiga).ToHashSet();
    }

    /// <summary>Team-Id des Meisters der letzten VOLLSTAENDIG abgeschlossenen Saison dieses Wettbewerbs (= Titelverteidiger der aktuellen Saison), fuer die Meisterschale (APP_STRUCTURE.md Abschnitt 1b). Null wenn es keine abgeschlossene Vorsaison gibt.</summary>
    private async Task<int?> ErmittleTitelverteidigerAsync(int wettbewerbId, Saison aktuelleSaison, CancellationToken ct)
    {
        var vorSaison = await db.Saisons.AsNoTracking()
            .Where(s => s.WettbewerbId == wettbewerbId && s.Startjahr < aktuelleSaison.Startjahr)
            .OrderByDescending(s => s.Startjahr)
            .FirstOrDefaultAsync(ct);

        if (vorSaison is null)
        {
            return null;
        }

        var vorSaisonSpiele = await db.Spiele.AsNoTracking()
            .Include(s => s.HeimTeam)
            .Include(s => s.AuswaertsTeam)
            .Where(s => s.SaisonId == vorSaison.Id && s.Status == SpielStatus.Beendet && s.ToreHeim != null && s.ToreAuswaerts != null)
            .ToListAsync(ct);

        if (vorSaisonSpiele.Count == 0)
        {
            return null;
        }

        var vorSaisonTabelle = BuildTabelle(vorSaisonSpiele, null, null, []);
        return vorSaisonTabelle.FirstOrDefault(z => z.Platz == 1)?.TeamId;
    }

    /// <summary>Team-Id des aktuellen DFB-Pokalsiegers (letztes vollstaendig gespieltes Turnier, hoechste SpieltagNummer = Finale), nur fuer 1./2. Bundesliga (APP_STRUCTURE.md Abschnitt 1b). Wettbewerb 4 = DFB-Pokal, siehe PREDICTION_FACTORS.md "Praezisierung zum DFB-Pokal" - dient ausschliesslich dieser Ermittlung, kein reaktivierter Vorhersage-Wettbewerb.</summary>
    private const int DfbPokalWettbewerbId = 4;

    private async Task<int?> ErmittlePokalsiegerAsync(int wettbewerbId, CancellationToken ct)
    {
        if (wettbewerbId != 1 && wettbewerbId != 2)
        {
            return null;
        }

        // Letzte Saison mit einem beendeten Finale (hoechste SpieltagNummer dieser Saison = Endspiel,
        // siehe SpielConfiguration/Import: OpenLigaDB liefert die Pokal-Rundenreihenfolge bereits als
        // aufsteigende GroupOrderId).
        var saisonsMitSpielen = await db.Spiele.AsNoTracking()
            .Where(s => s.WettbewerbId == DfbPokalWettbewerbId && s.Status == SpielStatus.Beendet)
            .Select(s => s.SaisonId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var saisonId in saisonsMitSpielen)
        {
            var maxRunde = await db.Spiele.AsNoTracking()
                .Where(s => s.WettbewerbId == DfbPokalWettbewerbId && s.SaisonId == saisonId)
                .MaxAsync(s => (int?)s.SpieltagNummer, ct);

            if (maxRunde is null)
            {
                continue;
            }

            var finale = await db.Spiele.AsNoTracking()
                .Where(s => s.WettbewerbId == DfbPokalWettbewerbId && s.SaisonId == saisonId && s.SpieltagNummer == maxRunde
                            && s.Status == SpielStatus.Beendet && s.ToreHeim != null && s.ToreAuswaerts != null)
                .OrderByDescending(s => s.Anstoss)
                .FirstOrDefaultAsync(ct);

            if (finale is not null)
            {
                // Nur eindeutig werten, wenn es tatsaechlich genau EIN Spiel in der hoechsten Runde
                // gibt (Finale) - Zwischenrunden haben mehrere Partien in derselben Runde.
                var anzahlInHoechsterRunde = await db.Spiele.AsNoTracking()
                    .CountAsync(s => s.WettbewerbId == DfbPokalWettbewerbId && s.SaisonId == saisonId && s.SpieltagNummer == maxRunde, ct);

                if (anzahlInHoechsterRunde == 1)
                {
                    return finale.ToreHeim!.Value > finale.ToreAuswaerts!.Value ? finale.HeimTeamId : finale.AuswaertsTeamId;
                }
            }
        }

        return null;
    }

    /// <summary>GET /api/tabelle/{wettbewerbId}/pokal - Rundenuebersicht fuer den DFB-Pokal (APP_STRUCTURE.md: "kompakte Turnierbaum-/Rundenansicht").</summary>
    [HttpGet("{wettbewerbId:int}/pokal")]
    public async Task<ActionResult<PokalUebersichtDto>> GetPokalUebersicht(int wettbewerbId, [FromQuery] int? saisonId, CancellationToken ct)
    {
        var wettbewerb = await db.Wettbewerbe.AsNoTracking().FirstOrDefaultAsync(w => w.Id == wettbewerbId, ct);
        if (wettbewerb is null)
        {
            return NotFound(new { error = $"Wettbewerb mit Id {wettbewerbId} nicht gefunden." });
        }

        var saison = await ResolveSaisonAsync(wettbewerbId, saisonId, ct);
        if (saison is null)
        {
            return Ok(new PokalUebersichtDto(wettbewerb.Id, wettbewerb.Name, []));
        }

        var spiele = await db.Spiele
            .AsNoTracking()
            .Include(s => s.Wettbewerb)
            .Include(s => s.HeimTeam)
            .Include(s => s.AuswaertsTeam)
            .Include(s => s.RadioLink)
            .Include(s => s.Vorhersagen.Where(v => v.IstAktuell))
            .Where(s => s.SaisonId == saison.Id)
            .OrderBy(s => s.SpieltagNummer)
            .ThenBy(s => s.Anstoss)
            .ToListAsync(ct);

        var runden = spiele
            .GroupBy(s => new { s.SpieltagNummer, s.SpieltagName })
            .OrderBy(g => g.Key.SpieltagNummer)
            .Select(g => new PokalRundeDto(
                g.Key.SpieltagName ?? $"Runde {g.Key.SpieltagNummer}",
                g.Key.SpieltagNummer,
                [.. g.Select(s => s.ToListeDto())]))
            .ToList();

        return Ok(new PokalUebersichtDto(wettbewerb.Id, wettbewerb.Name, runden));
    }

    private async Task<Saison?> ResolveSaisonAsync(int wettbewerbId, int? saisonId, CancellationToken ct)
    {
        if (saisonId.HasValue)
        {
            return await db.Saisons.AsNoTracking().FirstOrDefaultAsync(s => s.Id == saisonId.Value && s.WettbewerbId == wettbewerbId, ct);
        }

        return await db.Saisons.AsNoTracking()
            .Where(s => s.WettbewerbId == wettbewerbId)
            .OrderByDescending(s => s.IstAktuell)
            .ThenByDescending(s => s.Startjahr)
            .FirstOrDefaultAsync(ct);
    }

    private static List<TabellenZeileDto> BuildTabelle(List<Spiel> spiele, int? titelverteidigerTeamId, int? pokalsiegerTeamId, ICollection<int> aufsteigerTeamIds)
    {
        var stats = new Dictionary<int, (Team Team, int Spiele, int Siege, int Unentschieden, int Niederlagen, int ToreGeschossen, int ToreKassiert)>();

        void Merge(Team team, int siege, int unentschieden, int niederlagen, int geschossen, int kassiert)
        {
            stats.TryGetValue(team.Id, out var bisherig);
            stats[team.Id] = (
                team,
                bisherig.Spiele + 1,
                bisherig.Siege + siege,
                bisherig.Unentschieden + unentschieden,
                bisherig.Niederlagen + niederlagen,
                bisherig.ToreGeschossen + geschossen,
                bisherig.ToreKassiert + kassiert);
        }

        foreach (var spiel in spiele)
        {
            var toreHeim = spiel.ToreHeim!.Value;
            var toreAuswaerts = spiel.ToreAuswaerts!.Value;

            if (toreHeim > toreAuswaerts)
            {
                Merge(spiel.HeimTeam, 1, 0, 0, toreHeim, toreAuswaerts);
                Merge(spiel.AuswaertsTeam, 0, 0, 1, toreAuswaerts, toreHeim);
            }
            else if (toreHeim < toreAuswaerts)
            {
                Merge(spiel.HeimTeam, 0, 0, 1, toreHeim, toreAuswaerts);
                Merge(spiel.AuswaertsTeam, 1, 0, 0, toreAuswaerts, toreHeim);
            }
            else
            {
                Merge(spiel.HeimTeam, 0, 1, 0, toreHeim, toreAuswaerts);
                Merge(spiel.AuswaertsTeam, 0, 1, 0, toreAuswaerts, toreHeim);
            }
        }

        var formProTeam = BuildForm(spiele);
        var vorherigePlatzierungProTeam = BuildVorherigePlatzierung(spiele);

        return [.. stats.Values
            .Select(s => new
            {
                s.Team,
                s.Spiele,
                s.Siege,
                s.Unentschieden,
                s.Niederlagen,
                s.ToreGeschossen,
                s.ToreKassiert,
                Tordifferenz = s.ToreGeschossen - s.ToreKassiert,
                Punkte = s.Siege * 3 + s.Unentschieden,
            })
            .OrderByDescending(s => s.Punkte)
            .ThenByDescending(s => s.Tordifferenz)
            .ThenByDescending(s => s.ToreGeschossen)
            .Select((s, index) => new TabellenZeileDto(
                index + 1,
                s.Team.Id,
                s.Team.Name,
                s.Team.Kurzname,
                s.Team.LogoUrl,
                s.Spiele,
                s.Siege,
                s.Unentschieden,
                s.Niederlagen,
                s.ToreGeschossen,
                s.ToreKassiert,
                s.Tordifferenz,
                s.Punkte,
                formProTeam.TryGetValue(s.Team.Id, out var form) ? form : [],
                vorherigePlatzierungProTeam.TryGetValue(s.Team.Id, out var vorherigerPlatz) ? vorherigerPlatz : null,
                s.Team.Id == titelverteidigerTeamId,
                s.Team.Id == pokalsiegerTeamId,
                aufsteigerTeamIds.Contains(s.Team.Id)))];
    }

    /// <summary>
    /// Tabellenplatz je Team NACH dem vorletzten abgeschlossenen Spieltag, fuer die Trend-Anzeige
    /// (APP_STRUCTURE.md Abschnitt 1b: "3. Bundesliga: ... 3. (5.)"). Ermittelt den hoechsten
    /// SpieltagNummer-Wert unter den uebergebenen (bereits auf Status=Beendet gefilterten) Spielen
    /// und berechnet die Tabelle erneut MIT DENSELBEN Aggregationsregeln wie BuildTabelle, aber
    /// ohne die Spiele dieses letzten Spieltags - kein neuer Datenbedarf, reine Neuberechnung aus
    /// der ohnehin schon geladenen Saison-Spieleliste. Liefert leeres Dictionary (= ueberall null),
    /// wenn es nur einen einzigen abgeschlossenen Spieltag gibt (kein "vorher" existiert).
    /// </summary>
    private static Dictionary<int, int> BuildVorherigePlatzierung(List<Spiel> spiele)
    {
        if (spiele.Count == 0)
        {
            return [];
        }

        var letzterSpieltag = spiele.Max(s => s.SpieltagNummer);
        var vorherigeSpiele = spiele.Where(s => s.SpieltagNummer != letzterSpieltag).ToList();

        if (vorherigeSpiele.Count == 0)
        {
            return [];
        }

        var stats = new Dictionary<int, (int Punkte, int ToreGeschossen, int Tordifferenz)>();

        void Merge(int teamId, int punkte, int geschossen, int differenz)
        {
            stats.TryGetValue(teamId, out var bisherig);
            stats[teamId] = (bisherig.Punkte + punkte, bisherig.ToreGeschossen + geschossen, bisherig.Tordifferenz + differenz);
        }

        foreach (var spiel in vorherigeSpiele)
        {
            var toreHeim = spiel.ToreHeim!.Value;
            var toreAuswaerts = spiel.ToreAuswaerts!.Value;
            var differenz = toreHeim - toreAuswaerts;

            if (toreHeim > toreAuswaerts)
            {
                Merge(spiel.HeimTeamId, 3, toreHeim, differenz);
                Merge(spiel.AuswaertsTeamId, 0, toreAuswaerts, -differenz);
            }
            else if (toreHeim < toreAuswaerts)
            {
                Merge(spiel.HeimTeamId, 0, toreHeim, differenz);
                Merge(spiel.AuswaertsTeamId, 3, toreAuswaerts, -differenz);
            }
            else
            {
                Merge(spiel.HeimTeamId, 1, toreHeim, differenz);
                Merge(spiel.AuswaertsTeamId, 1, toreAuswaerts, -differenz);
            }
        }

        return stats
            .OrderByDescending(kv => kv.Value.Punkte)
            .ThenByDescending(kv => kv.Value.Tordifferenz)
            .ThenByDescending(kv => kv.Value.ToreGeschossen)
            .Select((kv, index) => (kv.Key, Platz: index + 1))
            .ToDictionary(x => x.Key, x => x.Platz);
    }

    /// <summary>
    /// Form der letzten 5 Saison-Spiele je Team ("S"/"U"/"N", aeltestes zuerst) fuer die volle
    /// Live-Tabellen-Ansicht (APP_STRUCTURE.md Abschnitt 1b). Wird direkt aus der bereits
    /// geladenen Saison-Spieleliste abgeleitet statt ueber ITeamFormService, da dieser auf einen
    /// Stichzeitpunkt PRO Team ausgelegt ist (ein DB-Roundtrip je Team) - hier reicht eine
    /// einmalige chronologische Gruppierung der ohnehin schon vorliegenden Liste.
    /// </summary>
    private static Dictionary<int, List<string>> BuildForm(List<Spiel> spiele)
    {
        var chronologisch = spiele.OrderBy(s => s.Anstoss).ToList();
        var proTeam = new Dictionary<int, List<string>>();

        void Append(int teamId, string ergebnis)
        {
            if (!proTeam.TryGetValue(teamId, out var liste))
            {
                liste = [];
                proTeam[teamId] = liste;
            }
            liste.Add(ergebnis);
            if (liste.Count > 5)
            {
                liste.RemoveAt(0);
            }
        }

        foreach (var spiel in chronologisch)
        {
            var toreHeim = spiel.ToreHeim!.Value;
            var toreAuswaerts = spiel.ToreAuswaerts!.Value;

            if (toreHeim > toreAuswaerts)
            {
                Append(spiel.HeimTeamId, "S");
                Append(spiel.AuswaertsTeamId, "N");
            }
            else if (toreHeim < toreAuswaerts)
            {
                Append(spiel.HeimTeamId, "N");
                Append(spiel.AuswaertsTeamId, "S");
            }
            else
            {
                Append(spiel.HeimTeamId, "U");
                Append(spiel.AuswaertsTeamId, "U");
            }
        }

        return proTeam;
    }
}
