namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Ein Schiedsrichter. Die aggregierten Karten-/Elfmeter-Schnittwerte aus PREDICTION_FACTORS.md
/// Abschnitt 9 werden bewusst NICHT hier als Spalte gefuehrt, sondern liegen in
/// <see cref="SchiedsrichterStatistik"/> - jeweils fuer einen Wettbewerb+Saison-Kontext, weil ein
/// "Liga-Durchschnitt" nur innerhalb eines Wettbewerbs/einer Saison sinnvoll vergleichbar ist
/// (ein Schiri, der ueberwiegend in der 3. Liga pfeift, braucht einen anderen Referenzwert als
/// einer, der meist 1.-Liga-Spiele leitet).
/// </summary>
public class Schiedsrichter
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int? OpenLigaDbSchiedsrichterId { get; set; }

    public ICollection<Spiel> Spiele { get; set; } = new List<Spiel>();
    public ICollection<SchiedsrichterStatistik> Statistiken { get; set; } = new List<SchiedsrichterStatistik>();
}

/// <summary>
/// Aggregierte Schiedsrichter-Kennzahlen fuer einen Wettbewerb+Saison-Kontext (Karten-/
/// Elfmeter-Schnitt im Vergleich zum Liga-Durchschnitt, PREDICTION_FACTORS.md Abschnitt 9). Wird
/// periodisch aus den Einzelspielen neu berechnet (Batch-Job, kein Live-Trigger noetig - Werte
/// aendern sich nur nach abgeschlossenen Spielen), daher als eigene aggregierte Tabelle statt
/// Laufzeitberechnung ueber alle Spiele hinweg.
/// </summary>
public class SchiedsrichterStatistik
{
    public int Id { get; set; }

    public int SchiedsrichterId { get; set; }
    public Schiedsrichter Schiedsrichter { get; set; } = null!;

    public int WettbewerbId { get; set; }
    public Wettbewerb Wettbewerb { get; set; } = null!;

    public int SaisonId { get; set; }
    public Saison Saison { get; set; } = null!;

    public int AnzahlSpiele { get; set; }

    public decimal GelbeKartenSchnitt { get; set; }
    public decimal GelbRoteKartenSchnitt { get; set; }
    public decimal RoteKartenSchnitt { get; set; }
    public decimal ElfmeterSchnitt { get; set; }

    /// <summary>Zeitpunkt der letzten Neuberechnung dieser aggregierten Zeile.</summary>
    public DateTimeOffset BerechnetAm { get; set; }
}
