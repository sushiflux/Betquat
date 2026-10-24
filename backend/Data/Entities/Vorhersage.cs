using System.Text.Json;

namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Vorhersage fuer ein Spiel - muss laut PREDICTION_FACTORS.md ("Ausgabeform") vor Anpfiff
/// berechnet werden und eine vollstaendige Wahrscheinlichkeitsverteilung ueber plausible
/// Ergebnisse liefern, nicht nur einen einzelnen Tipp. Ein Spiel kann mehrere Vorhersagen ueber
/// die Zeit haben (Modell-Iterationen/Neuberechnungen vor Anpfiff) - deshalb 1:n statt 1:1, mit
/// <see cref="IstAktuell"/> als Marker fuer die zuletzt gueltige.
/// </summary>
public class Vorhersage
{
    public int Id { get; set; }

    public int SpielId { get; set; }
    public Spiel Spiel { get; set; } = null!;

    /// <summary>Zeitpunkt der Berechnung - muss vor Spiel.Anstoss liegen (Konstraint in DB, siehe Configuration).</summary>
    public DateTimeOffset BerechnetAm { get; set; }

    /// <summary>Modell-/Versionskennung, die diese Vorhersage erzeugt hat (z.B. "poisson-dixon-coles-v1", "gbm-v2") - fuer spaeteres Backtesting mehrerer Modellversionen nebeneinander.</summary>
    public required string ModellVersion { get; set; }

    /// <summary>Haupttipp: Tore Heimteam (das wahrscheinlichste Einzelergebnis).</summary>
    public int TippToreHeim { get; set; }

    /// <summary>Haupttipp: Tore Auswaertsteam.</summary>
    public int TippToreAuswaerts { get; set; }

    /// <summary>Wahrscheinlichkeit des Haupttipps, 0-1 (z.B. 0.13 fuer "13%") - denormalisiert aus der vollen Verteilung fuer schnellen Zugriff ohne JSON-Parsing.</summary>
    public decimal TippWahrscheinlichkeit { get; set; }

    /// <summary>1X2-Wahrscheinlichkeit Heimsieg, 0-1 - aus derselben Toreverteilung abgeleitetes Nebenprodukt (PREDICTION_FACTORS.md "Vorhersageziel").</summary>
    public decimal WahrscheinlichkeitHeimsieg { get; set; }

    /// <summary>1X2-Wahrscheinlichkeit Remis, 0-1.</summary>
    public decimal WahrscheinlichkeitRemis { get; set; }

    /// <summary>1X2-Wahrscheinlichkeit Auswaertssieg, 0-1.</summary>
    public decimal WahrscheinlichkeitAuswaertssieg { get; set; }

    /// <summary>Over-2.5-Wahrscheinlichkeit, 0-1 - Nebenprodukt derselben Verteilung.</summary>
    public decimal? WahrscheinlichkeitUeber25 { get; set; }

    /// <summary>Beide-Teams-Treffen-Wahrscheinlichkeit, 0-1 - Nebenprodukt derselben Verteilung.</summary>
    public decimal? WahrscheinlichkeitBeideTreffen { get; set; }

    /// <summary>
    /// Vollstaendige Wahrscheinlichkeitsverteilung ueber alle betrachteten Ergebnisse als JSON,
    /// z.B. [{"heim":2,"auswaerts":1,"wahrscheinlichkeit":0.13}, {"heim":1,"auswaerts":1,"wahrscheinlichkeit":0.11}, ...].
    /// JSONB statt eigener Zeilen-pro-Ergebnis-Tabelle: die Verteilung wird immer als Ganzes pro
    /// Vorhersage gelesen/geschrieben (nie einzeiliges Filtern/Aggregieren ueber Ergebnis-Zeilen
    /// hinweg noetig), ist von der Modell-Berechnung her ohnehin ein zusammenhaengendes Objekt,
    /// und die Kartenansicht (APP_STRUCTURE.md) zeigt nur Top-3-4 direkt aus demselben Objekt -
    /// eine Normalisierung waere hier reiner Overhead ohne Abfragevorteil.
    /// </summary>
    public required JsonDocument Ergebnisverteilung { get; set; }

    /// <summary>
    /// Snapshot der zum Berechnungszeitpunkt vorliegenden Einflussfaktoren als JSON (Form,
    /// Elo-Werte, Verletzungen, H2H, etc.) - Grundlage fuer die "Begruendung/Nachvollziehbarkeit
    /// der Vorhersage" im Spiel-Detail (APP_STRUCTURE.md Abschnitt 2: "welche Faktoren ... wie
    /// stark eingeflossen sind"). Bewusst ein Snapshot statt Live-Nachberechnung, weil sich Form/
    /// Verletzungen/Elo nach dem Spiel weiterentwickeln - die Begruendung muss den Stand VOR
    /// Anpfiff zeigen, nicht den heutigen.
    /// </summary>
    public required JsonDocument FaktorenSnapshot { get; set; }

    /// <summary>Markiert die zuletzt gueltige Vorhersage fuer dieses Spiel (bei mehreren Neuberechnungen vor Anpfiff) - eindeutig pro Spiel via partiellem Unique-Index, siehe Configuration.</summary>
    public bool IstAktuell { get; set; } = true;

    /// <summary>
    /// Konfidenz-Markierung (PREDICTION_FACTORS.md "Konfidenz-Markierung", vom Nutzer ergaenzt,
    /// bewusst OHNE Wettbezug): true, wenn die eigene Ergebnisverteilung ungewoehnlich "eindeutig"
    /// ist (siehe PredictionService fuer die konkrete Schwellenwert-Logik). Reines Signal aus der
    /// Modell-eigenen Sicherheit heraus ("dieses Spiel schaetzt unser Modell ungewoehnlich klar
    /// ein"), KEIN Abgleich mit Buchmacher-Quoten (waere Value-Betting-Logik, explizit nicht
    /// gewollt). Denormalisiert gespeichert statt bei jedem Request aus der Ergebnisverteilung neu
    /// berechnet, damit der Wochen-Feed dieses Flag ohne JSON-Parsing direkt filtern/anzeigen kann.
    /// </summary>
    public bool IstHoheKonfidenz { get; set; }

    /// <summary>
    /// Numerischer Konfidenz-Score, 0-1 (z.B. Top-Ergebnis-Wahrscheinlichkeit oder Abstand zur
    /// zweitwahrscheinlichsten Tendenz, siehe PredictionService) - fuer eine feinere visuelle
    /// Abstufung im Frontend als nur das binaere <see cref="IstHoheKonfidenz"/>-Flag, z.B. eine
    /// Farbskala statt nur ein/aus. Bewusst kein kalibrierter Wahrscheinlichkeitswert im
    /// statistischen Sinn, sondern ein Heuristik-Score - siehe PredictionService-Kommentar.
    /// </summary>
    public decimal KonfidenzScore { get; set; }

    // --- Auswertung nach Spielende (siehe Trefferquote-Auswertung, TrefferquoteEintrag) ---

    /// <summary>True, sobald das Spiel beendet ist UND Tipp exakt dem Endergebnis entsprach.</summary>
    public bool? TrafExakt { get; set; }

    /// <summary>True, sobald das Spiel beendet ist UND die 1X2-Tendenz (Heimsieg/Remis/Auswaertssieg) korrekt war.</summary>
    public bool? TrafTendenz { get; set; }
}
