namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Ballbesitz-/Schuss-Kennzahlen fuer EIN Team in EINEM 15-Minuten-Segment eines Spiels
/// (PREDICTION_FACTORS.md Abschnitt 10, Sofascore als Quelle) - sechs Segmente
/// (0-15/15-30/30-45/45-60/60-75/75-90) x zwei Teams = bis zu 12 Zeilen pro Spiel, rein optional
/// befuellt. Getrennt von <see cref="XgStatistik"/>, weil Zeitsegmente eine eigene, feinere
/// Aufloesung sind (pro Segment statt pro Spiel) und nicht jede Quelle, die xG liefert, auch
/// Zeitsegment-Daten liefert (Understat/FBref liefern kein Segment-Breakdown, nur Sofascore).
/// </summary>
public class ZeitsegmentStatistik
{
    public int Id { get; set; }

    public int SpielId { get; set; }
    public Spiel Spiel { get; set; } = null!;

    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    /// <summary>Segmentstart in Minuten (0, 15, 30, 45, 60, 75) - Segment laeuft bis SegmentStart+15.</summary>
    public int SegmentStart { get; set; }

    public decimal? BallbesitzProzent { get; set; }
    public int? Schuesse { get; set; }
    public int? SchuesseAufsTor { get; set; }
    public int? GefaehrlicheAngriffe { get; set; }

    public int DatenquelleId { get; set; }
    public Datenquelle Datenquelle { get; set; } = null!;

    public DateTimeOffset ImportiertAm { get; set; }
}
