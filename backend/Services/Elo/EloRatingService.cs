using BetQuote.Api.Data;
using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BetQuote.Api.Services.Elo;

public interface IEloRatingService
{
    /// <summary>Berechnet nach einem beendeten Spiel das Elo-Rating beider beteiligten Teams neu und schreibt je einen EloRatingHistorie-Eintrag.</summary>
    Task UpdateRatingsForSpielAsync(int spielId, CancellationToken ct = default);

    /// <summary>Aktuellstes bekanntes Elo-Rating eines Teams VOR einem gegebenen Zeitpunkt (Modell-Input fuer die Vorhersage) - Startwert, falls noch keine Historie existiert.</summary>
    Task<decimal> GetCurrentRatingAsync(int teamId, DateTimeOffset vorZeitpunkt, CancellationToken ct = default);
}

/// <summary>
/// Eigenes Elo-Rating pro Team, laufend aus den Ergebnisdaten selbst berechnet (PREDICTION_FACTORS.md
/// Abschnitt 1: "ein einzelner, laufend nach jedem Spiel aktualisierter Staerkewert je Team ... mit
/// Torbonus"). Angelehnt an die bekannte clubelo.com-Formel: Standard-Elo-Erwartungswert plus
/// Torbonus (grosse Siege bewegen das Rating staerker) und Heimvorteil-Bonus auf die Erwartung.
/// </summary>
public class EloRatingService(BetQuoteDbContext db) : IEloRatingService
{
    /// <summary>Neutraler Startwert fuer Teams ohne Historie (Aufgabenstellung: "1500 als neutraler Startwert").</summary>
    public const decimal StartElo = 1500m;

    /// <summary>K-Faktor: Grundschrittweite je Spiel - 20 ist ein in der Fussball-Elo-Literatur (clubelo, World Football Elo Ratings) gebraeuchlicher Wert fuer Ligaspiele.</summary>
    private const decimal KFaktor = 20m;

    /// <summary>Heimvorteil in Elo-Punkten, wird der Heimmannschaft vor der Erwartungswert-Berechnung aufgeschlagen (PREDICTION_FACTORS.md Abschnitt 5: "Heimvorteil generell").</summary>
    private const decimal HeimvorteilElo = 65m;

    public async Task UpdateRatingsForSpielAsync(int spielId, CancellationToken ct = default)
    {
        var spiel = await db.Spiele.FirstOrDefaultAsync(s => s.Id == spielId, ct);
        if (spiel is null || spiel.Status != SpielStatus.Beendet || spiel.ToreHeim is null || spiel.ToreAuswaerts is null)
        {
            return;
        }

        var eloHeimVor = await GetCurrentRatingAsync(spiel.HeimTeamId, spiel.Anstoss, ct);
        var eloAuswaertsVor = await GetCurrentRatingAsync(spiel.AuswaertsTeamId, spiel.Anstoss, ct);

        var (eloHeimNach, eloAuswaertsNach) = BerechneNeueRatings(
            eloHeimVor, eloAuswaertsVor, spiel.ToreHeim.Value, spiel.ToreAuswaerts.Value);

        db.EloRatingHistorien.Add(new EloRatingHistorie
        {
            TeamId = spiel.HeimTeamId,
            SpielId = spiel.Id,
            EloVorSpiel = eloHeimVor,
            EloNachSpiel = eloHeimNach,
            SpielZeitpunkt = spiel.Anstoss,
        });

        db.EloRatingHistorien.Add(new EloRatingHistorie
        {
            TeamId = spiel.AuswaertsTeamId,
            SpielId = spiel.Id,
            EloVorSpiel = eloAuswaertsVor,
            EloNachSpiel = eloAuswaertsNach,
            SpielZeitpunkt = spiel.Anstoss,
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task<decimal> GetCurrentRatingAsync(int teamId, DateTimeOffset vorZeitpunkt, CancellationToken ct = default)
    {
        var letzterEintrag = await db.EloRatingHistorien
            .Where(e => e.TeamId == teamId && e.SpielZeitpunkt < vorZeitpunkt)
            .OrderByDescending(e => e.SpielZeitpunkt)
            .FirstOrDefaultAsync(ct);

        return letzterEintrag?.EloNachSpiel ?? StartElo;
    }

    /// <summary>
    /// Standard-Elo-Erwartungswertformel, Ergebnis-Score (1/0.5/0) plus Tordifferenz-Bonus-Multiplikator
    /// (angelehnt an die von clubelo/World Football Elo Ratings verwendete Gewichtung: hoehere
    /// Tordifferenz bewegt das Rating ueberproportional, aber mit abnehmendem Grenzertrag).
    /// </summary>
    private static (decimal EloHeimNach, decimal EloAuswaertsNach) BerechneNeueRatings(
        decimal eloHeimVor, decimal eloAuswaertsVor, int toreHeim, int toreAuswaerts)
    {
        var eloDiff = (double)(eloHeimVor + HeimvorteilElo - eloAuswaertsVor);
        var erwartungHeim = 1.0 / (1.0 + Math.Pow(10, -eloDiff / 400.0));
        var erwartungAuswaerts = 1.0 - erwartungHeim;

        double ergebnisHeim;
        if (toreHeim > toreAuswaerts) ergebnisHeim = 1.0;
        else if (toreHeim < toreAuswaerts) ergebnisHeim = 0.0;
        else ergebnisHeim = 0.5;

        var torDifferenz = Math.Abs(toreHeim - toreAuswaerts);
        var torBonus = torDifferenz switch
        {
            0 or 1 => 1.0,
            2 => 1.5,
            _ => (11.0 + torDifferenz) / 8.0, // clubelo-aehnliche Formel fuer Differenzen >= 3
        };

        var aenderung = (decimal)(KFaktor_AsDouble() * torBonus * (ergebnisHeim - erwartungHeim));
        var eloHeimNach = eloHeimVor + aenderung;
        var eloAuswaertsNach = eloAuswaertsVor - aenderung;

        return (Math.Round(eloHeimNach, 2), Math.Round(eloAuswaertsNach, 2));
    }

    private static double KFaktor_AsDouble() => (double)KFaktor;
}
