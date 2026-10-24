namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Ein Spieler, aktuell einem Team zugeordnet (einfaches Modell: kein historisches
/// Transferverlauf-Tracking, da PREDICTION_FACTORS.md dafuer keinen Bedarf nennt - lediglich
/// "spielt die Stammelf" / Verletzungen / Torschuetzen-Zuordnung werden gebraucht).
/// </summary>
public class Spieler
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    /// <summary>Position, grob (Torwart/Abwehr/Mittelfeld/Sturm) - fuer Schluesselspieler-Einordnung bei Verletzungen.</summary>
    public string? Position { get; set; }

    public ICollection<SpielerVerletzung> Verletzungen { get; set; } = new List<SpielerVerletzung>();
}
