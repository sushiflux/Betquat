using BetQuote.Api.Data;
using BetQuote.Api.Data.Entities;
using BetQuote.Api.Services.Elo;
using Microsoft.EntityFrameworkCore;

namespace BetQuote.Api.Services.Prediction;

public interface ITeamFormService
{
    /// <summary>Formkennzahlen eines Teams aus den letzten N Spielen VOR einem Stichzeitpunkt (alle Wettbewerbe gemischt, wie in PREDICTION_FACTORS.md Abschnitt 1 beschrieben - kein Splitting nach Wettbewerb, da alle 4 Ligen derselben Spielklasse/Verein zuzurechnen sind).</summary>
    Task<TeamFormSnapshot> GetFormAsync(int teamId, int wettbewerbId, int saisonId, DateTimeOffset vorZeitpunkt, int anzahlSpiele = 8, CancellationToken ct = default);

    /// <summary>Head-to-Head-Historie der letzten N direkten Duelle beider Teams (beliebige Heim/Auswaerts-Konstellation) vor einem Stichzeitpunkt.</summary>
    Task<List<Spiel>> GetHeadToHeadAsync(int teamAId, int teamBId, DateTimeOffset vorZeitpunkt, int anzahl = 10, CancellationToken ct = default);
}

/// <summary>
/// Ermittelt die 🔴-Kernfaktoren aus PREDICTION_FACTORS.md Abschnitt 1 ("Team-Form & historische
/// Ergebnisse") direkt aus den bereits importierten Spieldaten - keine externe Quelle noetig.
/// </summary>
public class TeamFormService(BetQuoteDbContext db, IEloRatingService eloService) : ITeamFormService
{
    public async Task<TeamFormSnapshot> GetFormAsync(int teamId, int wettbewerbId, int saisonId, DateTimeOffset vorZeitpunkt, int anzahlSpiele = 8, CancellationToken ct = default)
    {
        var letzteSpiele = await db.Spiele
            .AsNoTracking()
            .Where(s => (s.HeimTeamId == teamId || s.AuswaertsTeamId == teamId)
                        && s.Status == SpielStatus.Beendet
                        && s.Anstoss < vorZeitpunkt
                        && s.ToreHeim != null && s.ToreAuswaerts != null)
            .OrderByDescending(s => s.Anstoss)
            .Take(anzahlSpiele)
            .ToListAsync(ct);

        var elo = await eloService.GetCurrentRatingAsync(teamId, vorZeitpunkt, ct);
        var tabellenposition = await GetTabellenpositionAsync(teamId, wettbewerbId, saisonId, vorZeitpunkt, ct);

        if (letzteSpiele.Count == 0)
        {
            return new TeamFormSnapshot(teamId, 0, 1.3m, 1.3m, 1.3m, 1.3m, elo, tabellenposition, 0, []);
        }

        var geschossen = new List<int>();
        var kassiert = new List<int>();
        var geschossenHeimAusw = new List<int>();
        var kassiertHeimAusw = new List<int>();
        var formkurve = new List<string>();
        var punkte = 0;

        foreach (var spiel in letzteSpiele)
        {
            var istHeim = spiel.HeimTeamId == teamId;
            var eigeneTore = istHeim ? spiel.ToreHeim!.Value : spiel.ToreAuswaerts!.Value;
            var gegnerTore = istHeim ? spiel.ToreAuswaerts!.Value : spiel.ToreHeim!.Value;

            geschossen.Add(eigeneTore);
            kassiert.Add(gegnerTore);

            // "Form-Split": Heimteam-Tore nur aus Heimspielen, Auswaertsteam-Tore nur aus
            // Auswaertsspielen (PREDICTION_FACTORS.md Abschnitt 1 "Form-Split").
            geschossenHeimAusw.Add(eigeneTore);
            kassiertHeimAusw.Add(gegnerTore);

            if (formkurve.Count < 5)
            {
                formkurve.Add(eigeneTore > gegnerTore ? "S" : eigeneTore < gegnerTore ? "N" : "U");
                if (formkurve.Count <= 5)
                {
                    punkte += eigeneTore > gegnerTore ? 3 : eigeneTore == gegnerTore ? 1 : 0;
                }
            }
        }

        return new TeamFormSnapshot(
            teamId,
            letzteSpiele.Count,
            Math.Round((decimal)geschossen.Average(), 3),
            Math.Round((decimal)kassiert.Average(), 3),
            Math.Round((decimal)geschossenHeimAusw.Average(), 3),
            Math.Round((decimal)kassiertHeimAusw.Average(), 3),
            elo,
            tabellenposition,
            punkte,
            formkurve);
    }

    public async Task<List<Spiel>> GetHeadToHeadAsync(int teamAId, int teamBId, DateTimeOffset vorZeitpunkt, int anzahl = 10, CancellationToken ct = default)
    {
        return await db.Spiele
            .AsNoTracking()
            .Where(s => s.Status == SpielStatus.Beendet
                        && s.Anstoss < vorZeitpunkt
                        && ((s.HeimTeamId == teamAId && s.AuswaertsTeamId == teamBId)
                            || (s.HeimTeamId == teamBId && s.AuswaertsTeamId == teamAId)))
            .OrderByDescending(s => s.Anstoss)
            .Take(anzahl)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Grobe Tabellenposition aus den bisherigen Saisonspielen (3-1-0-Punktesystem), Stand VOR
    /// dem Stichzeitpunkt - fuer den DFB-Pokal (kein Ligamodus) wird 0 zurueckgegeben, da dort
    /// keine Tabelle existiert (siehe Wettbewerb.IstPokal).
    /// </summary>
    private async Task<int> GetTabellenpositionAsync(int teamId, int wettbewerbId, int saisonId, DateTimeOffset vorZeitpunkt, CancellationToken ct)
    {
        var wettbewerb = await db.Wettbewerbe.AsNoTracking().FirstOrDefaultAsync(w => w.Id == wettbewerbId, ct);
        if (wettbewerb is { IstPokal: true })
        {
            return 0;
        }

        var saisonSpiele = await db.Spiele
            .AsNoTracking()
            .Where(s => s.SaisonId == saisonId
                        && s.Status == SpielStatus.Beendet
                        && s.Anstoss < vorZeitpunkt
                        && s.ToreHeim != null && s.ToreAuswaerts != null)
            .ToListAsync(ct);

        if (saisonSpiele.Count == 0)
        {
            return 0;
        }

        var punkteProTeam = new Dictionary<int, (int Punkte, int TorDiff)>();

        void AddResult(int tId, int punkte, int torDiff)
        {
            if (!punkteProTeam.TryGetValue(tId, out var bisherig))
            {
                bisherig = (0, 0);
            }
            punkteProTeam[tId] = (bisherig.Punkte + punkte, bisherig.TorDiff + torDiff);
        }

        foreach (var spiel in saisonSpiele)
        {
            var diff = spiel.ToreHeim!.Value - spiel.ToreAuswaerts!.Value;
            if (diff > 0)
            {
                AddResult(spiel.HeimTeamId, 3, diff);
                AddResult(spiel.AuswaertsTeamId, 0, -diff);
            }
            else if (diff < 0)
            {
                AddResult(spiel.HeimTeamId, 0, diff);
                AddResult(spiel.AuswaertsTeamId, 3, -diff);
            }
            else
            {
                AddResult(spiel.HeimTeamId, 1, 0);
                AddResult(spiel.AuswaertsTeamId, 1, 0);
            }
        }

        if (!punkteProTeam.ContainsKey(teamId))
        {
            return 0;
        }

        var tabelle = punkteProTeam
            .OrderByDescending(kv => kv.Value.Punkte)
            .ThenByDescending(kv => kv.Value.TorDiff)
            .Select((kv, index) => (TeamId: kv.Key, Platz: index + 1))
            .ToList();

        return tabelle.FirstOrDefault(t => t.TeamId == teamId).Platz;
    }
}
