namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Aggregierte Trefferquote fuer EINE Ebene der Drill-down-Hierarchie Gesamt -&gt; Saison -&gt;
/// Spieltag, JEWEILS pro Wettbewerb getrennt (APP_STRUCTURE.md Abschnitt 3 "Statistik-Widget":
/// "eigene Trefferquote-Zeile fuer 1. Bundesliga, 2. Bundesliga, 3. Liga, DFB-Pokal - jeweils
/// separat", "Spieltag -&gt; Saison -&gt; Gesamt als klare Drill-down-Hierarchie").
///
/// Design-Entscheidung (aggregierte Tabelle statt Materialized View oder Laufzeitberechnung):
/// - Eine Materialized View waere ausserhalb des EF-Core-Code-First-Migrationsflusses zu pflegen
///   (eigenes rohes SQL fuer CREATE MATERIALIZED VIEW + REFRESH, kein natives EF-Core-Konzept
///   dafuer) und haette denselben Nachteil wie eine reine Laufzeitberechnung: sie muesste bei
///   JEDEM Seitenaufruf ueber potenziell zehntausende Spiele/Vorhersagen aggregieren, obwohl sich
///   der zugrunde liegende Wert laut Fachvorgabe ohnehin nur einmal pro abgeschlossenem Spieltag
///   aendert ("Aktualisierung erst nach Abschluss eines Spieltags ... kann logischerweise erst
///   berechnet werden, wenn die Ergebnisse feststehen").
/// - Stattdessen: eine schlanke, vorab aggregierte Tabelle mit einer Zeile pro
///   (Wettbewerb, Ebene, Saison[, Spieltag]) - vom Statistik-Widget direkt per Index-Lookup lesbar
///   ("soll sich fluessig im Statistik-Widget anzeigen lassen", siehe Aufgabenstellung), neu
///   berechnet durch einen Batch-Schritt (Teil der Datenimport-Pipeline, siehe Ruecksprache-Punkt
///   unten) nach jedem abgeschlossenen Spieltag - NICHT bei jedem Request.
/// - Spieltag-Zeilen sind die Basis ("Saisonwerte setzen sich aus den Spieltag-Werten zusammen"),
///   Saison- und Gesamt-Zeilen sind Aufsummierungen der darunterliegenden Ebene, in derselben
///   Tabelle mit Ebene=Saison bzw. Ebene=Gesamt und SpieltagNummer=null gespeichert - eine Abfrage
///   auf eine feste Kombination aus Wettbewerb+Ebene+Saison liefert so immer eine einzelne Zeile
///   ohne Laufzeit-Aggregation ueber die Kind-Ebene.
/// </summary>
public class TrefferquoteEintrag
{
    public int Id { get; set; }

    public int WettbewerbId { get; set; }
    public Wettbewerb Wettbewerb { get; set; } = null!;

    public TrefferquoteEbene Ebene { get; set; }

    /// <summary>Bei Ebene=Saison oder Spieltag gesetzt; bei Ebene=Gesamt null (uebergreifend ueber alle Saisons dieses Wettbewerbs).</summary>
    public int? SaisonId { get; set; }
    public Saison? Saison { get; set; }

    /// <summary>Nur bei Ebene=Spieltag gesetzt.</summary>
    public int? SpieltagNummer { get; set; }

    /// <summary>Anzahl ausgewerteter Spiele (nur beendete Spiele mit vorliegender Vorhersage zaehlen).</summary>
    public int AnzahlSpiele { get; set; }

    public int AnzahlExakteTreffer { get; set; }

    /// <summary>AnzahlExakteTreffer / AnzahlSpiele, 0-1 - denormalisiert gespeichert statt nur zur Laufzeit aus den beiden Zaehlern berechnet, damit Sortierung/Vergleich ueber Zeilen hinweg ohne Rechenschritt moeglich ist.</summary>
    public decimal ExakteTrefferquote { get; set; }

    public int AnzahlTendenzTreffer { get; set; }

    public decimal TendenzTrefferquote { get; set; }

    /// <summary>Zeitpunkt der letzten Neuberechnung dieser Zeile (Batch-Lauf nach Spieltagabschluss).</summary>
    public DateTimeOffset BerechnetAm { get; set; }
}

public enum TrefferquoteEbene
{
    Gesamt = 0,
    Saison = 1,
    Spieltag = 2,
}
