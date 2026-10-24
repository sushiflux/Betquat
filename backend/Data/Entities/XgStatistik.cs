namespace BetQuote.Api.Data.Entities;

/// <summary>
/// xG/xGOT-Werte fuer EIN Team in EINEM Spiel (zwei Zeilen pro Spiel: Heim + Auswaerts). Rein
/// optional befuellt aus Understat/Sofascore/FBref (PREDICTION_FACTORS.md Abschnitt 2 + 10) - kein
/// Pflichtfeld, das Modell muss laut Fachvorgabe auch ganz ohne xG-Quelle funktionieren
/// (v.a. 2./3. Liga/Pokal, wo Understat/FBref oft nichts liefern).
/// </summary>
public class XgStatistik
{
    public int Id { get; set; }

    public int SpielId { get; set; }
    public Spiel Spiel { get; set; } = null!;

    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    /// <summary>Kennzeichnet, ob diese Zeile das Heim- oder Auswaertsteam betrifft - vereinfacht Spiel.XgStatistikHeim/Auswaerts-Navigation, siehe Configuration.</summary>
    public bool IstHeimteam { get; set; }

    public int DatenquelleId { get; set; }
    public Datenquelle Datenquelle { get; set; } = null!;

    /// <summary>Expected Goals - Chancenqualität vor dem Schuss.</summary>
    public decimal? Xg { get; set; }

    /// <summary>Expected Goals Against.</summary>
    public decimal? Xga { get; set; }

    /// <summary>Expected Goals on Target / Post-Shot-xG (PSxG bei FBref) - bewertet zusaetzlich den tatsaechlichen Schuss (Platzierung, Schaerfe).</summary>
    public decimal? Xgot { get; set; }

    public DateTimeOffset ImportiertAm { get; set; }
}
