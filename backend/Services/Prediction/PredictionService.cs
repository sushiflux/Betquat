using System.Text.Json;
using BetQuote.Api.Data;
using BetQuote.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BetQuote.Api.Services.Prediction;

public interface IPredictionService
{
    /// <summary>Berechnet (falls noch keine aktuelle Vorhersage existiert oder eine Neuberechnung gewuenscht ist) die Vorhersage fuer ein einzelnes Spiel. Wirft, falls das Spiel bereits angepfiffen ist (siehe Vorhersage.BerechnetAm-Dokumentation).</summary>
    Task<Vorhersage?> BerechneVorhersageAsync(int spielId, CancellationToken ct = default);

    /// <summary>Batch-Lauf: berechnet Vorhersagen fuer alle Spiele der naechsten <paramref name="tageVoraus"/> Tage, die noch keine aktuelle Vorhersage haben (Aufgabenstellung: "taeglich fuer alle Spiele der naechsten 7 Tage ohne aktuelle Vorhersage").</summary>
    Task<int> BerechneAusstehendeVorhersagenAsync(int tageVoraus = 7, CancellationToken ct = default);
}

/// <summary>
/// Orchestriert die Vorhersage-Berechnung: zieht Formfaktoren (<see cref="ITeamFormService"/>),
/// leitet daraus Torerwartungswerte (Lambda je Team) ab und uebergibt die eigentliche
/// Wahrscheinlichkeitsberechnung an <see cref="PoissonDixonColesModel"/>. Das Ergebnis wird als
/// Vorhersage-Entity inkl. FaktorenSnapshot (fuer die spaetere Begruendungs-Anzeige im Frontend)
/// persistiert.
///
/// WICHTIG (Aufgabenstellung): Vorhersage MUSS vor Anpfiff berechnet werden. Das laesst sich laut
/// database-Agent nicht als DB-Constraint erzwingen (Cross-Table-CHECK nicht moeglich, siehe
/// VorhersageConfiguration-Kommentar) - wird daher hier im Code hart durchgesetzt: fuer ein Spiel,
/// dessen Anstoss bereits vergangen/laufend/beendet ist, wird keine neue Vorhersage erzeugt.
/// </summary>
public class PredictionService(
    BetQuoteDbContext db,
    ITeamFormService formService,
    ILogger<PredictionService> logger) : IPredictionService
{
    private const string ModellVersion = "poisson-dixon-coles-v1";

    /// <summary>Liga-Baseline fuer die durchschnittliche Anzahl Tore pro Team und Spiel - Ausgangspunkt, wenn kaum/keine Formdaten vorliegen (frueher Saisonstart). Grober Wert ueber alle 4 deutschen Wettbewerbe hinweg (PREDICTION_FACTORS.md Abschnitt 8: "Liga-spezifische Baseline").</summary>
    private const decimal LigaBaselineTore = 1.35m;

    /// <summary>Ab diesem KonfidenzScore (0-1) gilt eine Vorhersage als "hohe Konfidenz" (Vorhersage.IstHoheKonfidenz) - Kalibrierungswert, keine harte fachliche Vorgabe (siehe PREDICTION_FACTORS.md "Konfidenz-Markierung": "Konkrete Schwellenwerte ... sind eine Kalibrierungsfrage fuer die Umsetzung"). Kann spaeter anhand von Backtesting-Ergebnissen nachjustiert werden.</summary>
    private const decimal HoheKonfidenzSchwelle = 0.6m;

    public async Task<Vorhersage?> BerechneVorhersageAsync(int spielId, CancellationToken ct = default)
    {
        var spiel = await db.Spiele
            .Include(s => s.HeimTeam)
            .Include(s => s.AuswaertsTeam)
            .FirstOrDefaultAsync(s => s.Id == spielId, ct);

        if (spiel is null)
        {
            logger.LogWarning("BerechneVorhersageAsync: Spiel {SpielId} nicht gefunden", spielId);
            return null;
        }

        var berechnetAm = DateTimeOffset.UtcNow;

        // Harte Durchsetzung "vor Anpfiff" (siehe Klassendoku) - kein Ausnahmefall, einfach
        // ueberspringen statt Exception, da das im Batch-Lauf regulaer vorkommen kann (Spiel
        // zwischen Abfrage und Verarbeitung angepfiffen).
        if (berechnetAm >= spiel.Anstoss)
        {
            logger.LogInformation("Vorhersage fuer Spiel {SpielId} uebersprungen: Anstoss liegt nicht mehr in der Zukunft", spielId);
            return null;
        }

        var formHeim = await formService.GetFormAsync(spiel.HeimTeamId, spiel.WettbewerbId, spiel.SaisonId, spiel.Anstoss, ct: ct);
        var formAuswaerts = await formService.GetFormAsync(spiel.AuswaertsTeamId, spiel.WettbewerbId, spiel.SaisonId, spiel.Anstoss, ct: ct);
        var h2h = await formService.GetHeadToHeadAsync(spiel.HeimTeamId, spiel.AuswaertsTeamId, spiel.Anstoss, ct: ct);

        var (lambdaHeim, lambdaAuswaerts) = BerechneLambdas(formHeim, formAuswaerts, h2h, spiel.HeimTeamId);

        var ergebnis = PoissonDixonColesModel.Berechne(lambdaHeim, lambdaAuswaerts);
        var konfidenzScore = BerechneKonfidenzScore(ergebnis);

        var vorhandene = await db.Vorhersagen.FirstOrDefaultAsync(v => v.SpielId == spielId && v.IstAktuell, ct);
        if (vorhandene is not null)
        {
            vorhandene.IstAktuell = false;
        }

        var ergebnisverteilungJson = JsonSerializer.SerializeToDocument(
            ergebnis.Verteilung
                .Where(v => v.Wahrscheinlichkeit > 0.0001m) // Nur relevante Ergebnisse persistieren, nicht alle 81 Zellen der Matrix.
                .OrderByDescending(v => v.Wahrscheinlichkeit)
                .Select(v => new { heim = v.ToreHeim, auswaerts = v.ToreAuswaerts, wahrscheinlichkeit = v.Wahrscheinlichkeit }));

        var faktorenSnapshotJson = JsonSerializer.SerializeToDocument(new
        {
            heimteam = new
            {
                teamId = formHeim.TeamId,
                formSpiele = formHeim.AnzahlSpieleBetrachtet,
                toreGeschossenSchnitt = formHeim.DurchschnittGeschosseneTore,
                toreKassiertSchnitt = formHeim.DurchschnittKassierteTore,
                elo = formHeim.EloRating,
                tabellenposition = formHeim.Tabellenposition,
                formkurve = formHeim.FormkurveLetzte5,
                punkteLetzte5 = formHeim.PunkteLetzte5,
            },
            auswaertsteam = new
            {
                teamId = formAuswaerts.TeamId,
                formSpiele = formAuswaerts.AnzahlSpieleBetrachtet,
                toreGeschossenSchnitt = formAuswaerts.DurchschnittGeschosseneTore,
                toreKassiertSchnitt = formAuswaerts.DurchschnittKassierteTore,
                elo = formAuswaerts.EloRating,
                tabellenposition = formAuswaerts.Tabellenposition,
                formkurve = formAuswaerts.FormkurveLetzte5,
                punkteLetzte5 = formAuswaerts.PunkteLetzte5,
            },
            headToHead = new
            {
                anzahlBeruecksichtigt = h2h.Count,
                spiele = h2h.Select(s => new { s.Id, s.Anstoss, s.HeimTeamId, s.AuswaertsTeamId, s.ToreHeim, s.ToreAuswaerts }),
            },
            lambdaHeim = ergebnis.LambdaHeim,
            lambdaAuswaerts = ergebnis.LambdaAuswaerts,
            heimvorteilBeruecksichtigt = true,
            hinweis = "xG/xGOT nicht beruecksichtigt (optionales Modul nicht aktiv) - Berechnung basiert auf Form/Elo/Tabellenposition/H2H/Heimvorteil.",
        });

        var vorhersage = new Vorhersage
        {
            SpielId = spiel.Id,
            BerechnetAm = berechnetAm,
            ModellVersion = ModellVersion,
            TippToreHeim = ergebnis.Haupttipp.ToreHeim,
            TippToreAuswaerts = ergebnis.Haupttipp.ToreAuswaerts,
            TippWahrscheinlichkeit = ergebnis.Haupttipp.Wahrscheinlichkeit,
            WahrscheinlichkeitHeimsieg = ergebnis.WahrscheinlichkeitHeimsieg,
            WahrscheinlichkeitRemis = ergebnis.WahrscheinlichkeitRemis,
            WahrscheinlichkeitAuswaertssieg = ergebnis.WahrscheinlichkeitAuswaertssieg,
            WahrscheinlichkeitUeber25 = ergebnis.WahrscheinlichkeitUeber25,
            WahrscheinlichkeitBeideTreffen = ergebnis.WahrscheinlichkeitBeideTreffen,
            Ergebnisverteilung = ergebnisverteilungJson,
            FaktorenSnapshot = faktorenSnapshotJson,
            IstAktuell = true,
            KonfidenzScore = konfidenzScore,
            IstHoheKonfidenz = konfidenzScore >= HoheKonfidenzSchwelle,
        };

        db.Vorhersagen.Add(vorhersage);
        await db.SaveChangesAsync(ct);

        return vorhersage;
    }

    public async Task<int> BerechneAusstehendeVorhersagenAsync(int tageVoraus = 7, CancellationToken ct = default)
    {
        var jetzt = DateTimeOffset.UtcNow;
        var grenze = jetzt.AddDays(tageVoraus);

        var kandidaten = await db.Spiele
            .Where(s => s.Anstoss > jetzt
                        && s.Anstoss <= grenze
                        && s.Status == SpielStatus.Geplant
                        && !s.Vorhersagen.Any(v => v.IstAktuell))
            .Select(s => s.Id)
            .ToListAsync(ct);

        var erfolgreich = 0;
        foreach (var spielId in kandidaten)
        {
            try
            {
                var vorhersage = await BerechneVorhersageAsync(spielId, ct);
                if (vorhersage is not null)
                {
                    erfolgreich++;
                }
            }
            catch (Exception ex)
            {
                // Ein einzelnes Spiel darf den Batch-Lauf nicht abbrechen (gleiches Prinzip wie
                // beim Import).
                logger.LogWarning(ex, "Vorhersage-Berechnung fuer Spiel {SpielId} fehlgeschlagen, uebersprungen", spielId);
            }
        }

        return erfolgreich;
    }

    /// <summary>
    /// Leitet aus den Formkennzahlen beider Teams eine Torerwartung (Lambda) je Team ab -
    /// mindestens die 🔴-Faktoren aus PREDICTION_FACTORS.md (Form, H2H, geschossene/kassierte
    /// Tore, Elo, Tabellenposition, Heimvorteil), xG/xGOT bewusst ausgeklammert (Aufgabenstellung:
    /// "xG/xGOT koennen NULL sein, dann ohne diese Faktoren rechnen").
    ///
    /// Ansatz: attack/defence-Staerke relativ zur Liga-Baseline (aehnlich dem klassischen
    /// Dixon-Coles-Attack/Defence-Parameter-Ansatz, hier aber direkt aus Rohdaten statt aus einer
    /// vollen MLE-Schaetzung ueber die gesamte Liga - bewusst vereinfacht fuer eine "grobe aber
    /// funktionierende erste Version", siehe Aufgabenstellung "Realistische Erwartungshaltung").
    /// Elo-Differenz wirkt als zusaetzlicher multiplikativer Korrekturfaktor.
    /// </summary>
    private static (decimal LambdaHeim, decimal LambdaAuswaerts) BerechneLambdas(
        TeamFormSnapshot heim, TeamFormSnapshot auswaerts, List<Spiel> h2h, int heimTeamId)
    {
        // Basis-Torerwartung: eigene Offensive vs. gegnerische Defensive, gemittelt mit der
        // Liga-Baseline (Attack-Staerke = eigener Schnitt / Baseline, Defence-Schwaeche = gegnerischer
        // Kassiert-Schnitt / Baseline).
        var heimAngriff = SichererQuotient(heim.DurchschnittGeschosseneTore, LigaBaselineTore);
        var heimAbwehr = SichererQuotient(heim.DurchschnittKassierteTore, LigaBaselineTore);
        var auswaertsAngriff = SichererQuotient(auswaerts.DurchschnittGeschosseneTore, LigaBaselineTore);
        var auswaertsAbwehr = SichererQuotient(auswaerts.DurchschnittKassierteTore, LigaBaselineTore);

        var lambdaHeim = LigaBaselineTore * heimAngriff * auswaertsAbwehr;
        var lambdaAuswaerts = LigaBaselineTore * auswaertsAngriff * heimAbwehr;

        // Heimvorteil (PREDICTION_FACTORS.md Abschnitt 5) - empirisch ca. 10-15% mehr erwartete
        // Tore fuer die Heimmannschaft, entsprechend weniger fuer die Gastmannschaft.
        lambdaHeim *= 1.12m;
        lambdaAuswaerts *= 0.92m;

        // Elo-Differenz als zusaetzlicher Korrekturfaktor: 100 Elo-Punkte Vorsprung ~ moderater
        // Vorteil. Skaliert sanft (Sigmoid-aehnlich ueber einen linearen Ausschnitt), um Extremwerte
        // bei sehr grossen Elo-Unterschieden nicht zu ueberzeichnen.
        var eloDiff = heim.EloRating - auswaerts.EloRating;
        var eloFaktor = Math.Clamp(1m + eloDiff / 2000m, 0.75m, 1.25m);
        lambdaHeim *= eloFaktor;
        lambdaAuswaerts *= (2m - eloFaktor >= 0.75m && 2m - eloFaktor <= 1.25m) ? (2m - eloFaktor) : 1m;

        // Head-to-Head-Signal (Abschnitt 1: "letzte 5-10 Duelle"): leichte Verschiebung Richtung
        // des Teams, das die letzten direkten Duelle dominiert hat.
        if (h2h.Count > 0)
        {
            var heimSiege = h2h.Count(s => (s.HeimTeamId == heimTeamId && s.ToreHeim > s.ToreAuswaerts)
                                            || (s.AuswaertsTeamId == heimTeamId && s.ToreAuswaerts > s.ToreHeim));
            var auswaertsSiege = h2h.Count(s => (s.HeimTeamId == heimTeamId && s.ToreHeim < s.ToreAuswaerts)
                                                 || (s.AuswaertsTeamId == heimTeamId && s.ToreAuswaerts < s.ToreHeim));
            var h2hDiff = (decimal)(heimSiege - auswaertsSiege) / h2h.Count;
            lambdaHeim *= 1m + (h2hDiff * 0.08m);
            lambdaAuswaerts *= 1m - (h2hDiff * 0.08m);
        }

        // Sinnvolle Grenzen (keine Torerwartung nahe 0 oder unrealistisch hoch) - schuetzt vor
        // Ausreissern bei sehr kleinen Stichproben (frueher Saisonstart, wenige Vorspiele).
        lambdaHeim = Math.Clamp(lambdaHeim, 0.3m, 4.5m);
        lambdaAuswaerts = Math.Clamp(lambdaAuswaerts, 0.3m, 4.5m);

        return (Math.Round(lambdaHeim, 4), Math.Round(lambdaAuswaerts, 4));
    }

    private static decimal SichererQuotient(decimal wert, decimal baseline)
    {
        if (baseline <= 0) return 1m;
        return wert / baseline;
    }

    /// <summary>
    /// Konfidenz-Markierung (PREDICTION_FACTORS.md "Konfidenz-Markierung") - rein aus der Form der
    /// EIGENEN Ergebnisverteilung abgeleitet, bewusst OHNE jeden Abgleich mit Buchmacher-Quoten
    /// (das waere Value-Betting-Logik, laut Fachvorgabe explizit nicht gewollt). Kombiniert zwei
    /// Signale zu einem Score 0-1:
    /// 1. Wie hoch ist die Top-Ergebnis-Wahrscheinlichkeit selbst (ein Spiel mit 25% Top-Tipp ist
    ///    "eindeutiger" als eines mit 10%, bei ~380 moeglichen Ergebnissen in der Matrix).
    /// 2. Wie gross ist der Abstand zwischen der wahrscheinlichsten und zweitwahrscheinlichsten
    ///    1X2-Tendenz (ein klarer Favorit auf Heimsieg/Auswaertssieg/Remis ist eindeutiger als ein
    ///    knappes Rennen zwischen zwei Tendenzen).
    /// Bewusst eine einfache Heuristik statt eines statistisch strengen Konfidenzintervalls
    /// (Aufgabenstellung: "muss nicht perfekt kalibriert sein, kann spaeter verfeinert werden").
    /// </summary>
    private static decimal BerechneKonfidenzScore(VorhersageErgebnis ergebnis)
    {
        // Signal 1: Top-Ergebnis-Wahrscheinlichkeit, normalisiert auf eine Skala, bei der ~35%
        // (ungewoehnlich hoch fuer ein Einzelergebnis) bereits den Maximalwert 1 erreicht.
        var topErgebnisSignal = Math.Clamp(ergebnis.Haupttipp.Wahrscheinlichkeit / 0.35m, 0m, 1m);

        // Signal 2: Abstand zwischen staerkster und zweitstaerkster 1X2-Tendenz, normalisiert auf
        // eine Skala, bei der 50 Prozentpunkte Abstand (z.B. 70% Heimsieg vs. 20% Remis) den
        // Maximalwert 1 erreicht.
        var tendenzen = new[] { ergebnis.WahrscheinlichkeitHeimsieg, ergebnis.WahrscheinlichkeitRemis, ergebnis.WahrscheinlichkeitAuswaertssieg }
            .OrderDescending()
            .ToList();
        var tendenzAbstand = tendenzen[0] - tendenzen[1];
        var tendenzSignal = Math.Clamp(tendenzAbstand / 0.5m, 0m, 1m);

        // Gleich gewichteter Mittelwert beider Signale.
        return Math.Round((topErgebnisSignal + tendenzSignal) / 2m, 5);
    }
}
