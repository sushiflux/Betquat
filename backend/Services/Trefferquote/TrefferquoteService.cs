using BetQuote.Api.Data;
using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BetQuote.Api.Services.Trefferquote;

public interface ITrefferquoteService
{
    /// <summary>Traegt bei allen beendeten Spielen mit aktueller Vorhersage, die noch nicht ausgewertet wurden, TrafExakt/TrafTendenz nach.</summary>
    Task<int> AuswerteAbgeschlosseneVorhersagenAsync(CancellationToken ct = default);

    /// <summary>
    /// Aktualisiert die TrefferquoteEintrag-Zeilen (Spieltag/Saison/Gesamt, je Wettbewerb) fuer
    /// alle Spieltage, die seit dem letzten Lauf neu abgeschlossen wurden (Aufgabenstellung:
    /// "Nach Abschluss eines Spieltags ... TrefferquoteEintrag-Zeilen befuellen/aktualisieren").
    /// </summary>
    Task<int> AktualisiereAbgeschlosseneSpieltageAsync(CancellationToken ct = default);
}

/// <summary>
/// Batch-Job fuer die Trefferquote-Auswertung (APP_STRUCTURE.md Statistik-Widget). Laeuft in zwei
/// Schritten: (1) einzelne Vorhersagen gegen das tatsaechliche Endergebnis pruefen (exakt UND
/// 1X2-Tendenz getrennt, Aufgabenstellung), (2) daraus die aggregierten TrefferquoteEintrag-Zeilen
/// je Ebene (Spieltag -&gt; Saison -&gt; Gesamt) und Wettbewerb neu berechnen - siehe
/// TrefferquoteEintrag-Entity-Dokumentation fuer die Design-Begruendung der aggregierten Tabelle.
/// </summary>
public class TrefferquoteService(BetQuoteDbContext db, ILogger<TrefferquoteService> logger) : ITrefferquoteService
{
    public async Task<int> AuswerteAbgeschlosseneVorhersagenAsync(CancellationToken ct = default)
    {
        var auszuwerten = await db.Vorhersagen
            .Include(v => v.Spiel)
            .Where(v => v.IstAktuell
                        && v.TrafExakt == null
                        && v.Spiel.Status == SpielStatus.Beendet
                        && v.Spiel.ToreHeim != null && v.Spiel.ToreAuswaerts != null)
            .ToListAsync(ct);

        foreach (var vorhersage in auszuwerten)
        {
            var spiel = vorhersage.Spiel;
            var toreHeim = spiel.ToreHeim!.Value;
            var toreAuswaerts = spiel.ToreAuswaerts!.Value;

            vorhersage.TrafExakt = vorhersage.TippToreHeim == toreHeim && vorhersage.TippToreAuswaerts == toreAuswaerts;

            var tatsaechlicheTendenz = toreHeim > toreAuswaerts ? 1 : toreHeim < toreAuswaerts ? -1 : 0;
            var getippteTendenz = vorhersage.TippToreHeim > vorhersage.TippToreAuswaerts ? 1
                : vorhersage.TippToreHeim < vorhersage.TippToreAuswaerts ? -1 : 0;
            vorhersage.TrafTendenz = tatsaechlicheTendenz == getippteTendenz;
        }

        if (auszuwerten.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return auszuwerten.Count;
    }

    public async Task<int> AktualisiereAbgeschlosseneSpieltageAsync(CancellationToken ct = default)
    {
        // Ein Spieltag gilt als abgeschlossen, wenn alle seine Spiele beendet sind (Aufgabenstellung:
        // "alle Spiele eines Spieltags in einem Wettbewerb sind beendet"). Wir ermitteln alle
        // (Wettbewerb, Saison, Spieltag)-Kombinationen, bei denen das zutrifft.
        var abgeschlosseneSpieltage = await db.Spiele
            .GroupBy(s => new { s.WettbewerbId, s.SaisonId, s.SpieltagNummer })
            .Where(g => g.All(s => s.Status == SpielStatus.Beendet || s.Status == SpielStatus.Abgesagt)
                        && g.Any(s => s.Status == SpielStatus.Beendet))
            .Select(g => new { g.Key.WettbewerbId, g.Key.SaisonId, g.Key.SpieltagNummer })
            .ToListAsync(ct);

        var aktualisiert = 0;
        var betroffeneSaisons = new HashSet<(int WettbewerbId, int SaisonId)>();

        foreach (var spieltag in abgeschlosseneSpieltage)
        {
            try
            {
                await UpsertSpieltagEintragAsync(spieltag.WettbewerbId, spieltag.SaisonId, spieltag.SpieltagNummer, ct);
                betroffeneSaisons.Add((spieltag.WettbewerbId, spieltag.SaisonId));
                aktualisiert++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Trefferquote-Aktualisierung fuer Wettbewerb {WettbewerbId} Saison {SaisonId} Spieltag {Spieltag} fehlgeschlagen", spieltag.WettbewerbId, spieltag.SaisonId, spieltag.SpieltagNummer);
            }
        }

        foreach (var (wettbewerbId, saisonId) in betroffeneSaisons)
        {
            try
            {
                await UpsertSaisonEintragAsync(wettbewerbId, saisonId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Saison-Trefferquote-Aktualisierung fuer Wettbewerb {WettbewerbId} Saison {SaisonId} fehlgeschlagen", wettbewerbId, saisonId);
            }
        }

        foreach (var wettbewerbId in betroffeneSaisons.Select(b => b.WettbewerbId).Distinct())
        {
            try
            {
                await UpsertGesamtEintragAsync(wettbewerbId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Gesamt-Trefferquote-Aktualisierung fuer Wettbewerb {WettbewerbId} fehlgeschlagen", wettbewerbId);
            }
        }

        return aktualisiert;
    }

    private async Task UpsertSpieltagEintragAsync(int wettbewerbId, int saisonId, int spieltagNummer, CancellationToken ct)
    {
        var vorhersagen = await GetAusgewerteteVorhersagenAsync(
            v => v.Spiel.WettbewerbId == wettbewerbId && v.Spiel.SaisonId == saisonId && v.Spiel.SpieltagNummer == spieltagNummer, ct);

        await UpsertEintragAsync(wettbewerbId, TrefferquoteEbene.Spieltag, saisonId, spieltagNummer, vorhersagen, ct);
    }

    private async Task UpsertSaisonEintragAsync(int wettbewerbId, int saisonId, CancellationToken ct)
    {
        var vorhersagen = await GetAusgewerteteVorhersagenAsync(
            v => v.Spiel.WettbewerbId == wettbewerbId && v.Spiel.SaisonId == saisonId, ct);

        await UpsertEintragAsync(wettbewerbId, TrefferquoteEbene.Saison, saisonId, null, vorhersagen, ct);
    }

    private async Task UpsertGesamtEintragAsync(int wettbewerbId, CancellationToken ct)
    {
        var vorhersagen = await GetAusgewerteteVorhersagenAsync(
            v => v.Spiel.WettbewerbId == wettbewerbId, ct);

        await UpsertEintragAsync(wettbewerbId, TrefferquoteEbene.Gesamt, null, null, vorhersagen, ct);
    }

    private async Task<List<Vorhersage>> GetAusgewerteteVorhersagenAsync(
        System.Linq.Expressions.Expression<Func<Vorhersage, bool>> filter, CancellationToken ct)
    {
        return await db.Vorhersagen
            .Include(v => v.Spiel)
            .Where(v => v.IstAktuell && v.TrafExakt != null && v.TrafTendenz != null)
            .Where(filter)
            .ToListAsync(ct);
    }

    private async Task UpsertEintragAsync(
        int wettbewerbId, TrefferquoteEbene ebene, int? saisonId, int? spieltagNummer,
        List<Vorhersage> vorhersagen, CancellationToken ct)
    {
        var eintrag = await db.TrefferquoteEintraege.FirstOrDefaultAsync(
            t => t.WettbewerbId == wettbewerbId && t.Ebene == ebene && t.SaisonId == saisonId && t.SpieltagNummer == spieltagNummer, ct);

        if (eintrag is null)
        {
            eintrag = new TrefferquoteEintrag
            {
                WettbewerbId = wettbewerbId,
                Ebene = ebene,
                SaisonId = saisonId,
                SpieltagNummer = spieltagNummer,
            };
            db.TrefferquoteEintraege.Add(eintrag);
        }

        var anzahl = vorhersagen.Count;
        var exakteTreffer = vorhersagen.Count(v => v.TrafExakt == true);
        var tendenzTreffer = vorhersagen.Count(v => v.TrafTendenz == true);

        eintrag.AnzahlSpiele = anzahl;
        eintrag.AnzahlExakteTreffer = exakteTreffer;
        eintrag.ExakteTrefferquote = anzahl > 0 ? Math.Round((decimal)exakteTreffer / anzahl, 5) : 0;
        eintrag.AnzahlTendenzTreffer = tendenzTreffer;
        eintrag.TendenzTrefferquote = anzahl > 0 ? Math.Round((decimal)tendenzTreffer / anzahl, 5) : 0;
        eintrag.BerechnetAm = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
