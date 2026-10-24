namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Radio-/Audiostream-Link fuer ein Spiel (ARD-Sportschau-Scraping, siehe TECH_STACK.md/
/// APP_STRUCTURE.md "Radio-Link pro Spiel"). 1:1 zu Spiel, rein optional - nur fuer 1./2.
/// Bundesliga + DFB-Pokal verfuegbar, fuer 3. Liga bleibt die Zeile schlicht ungesetzt (kein
/// Fehlerzustand, siehe APP_STRUCTURE.md).
/// </summary>
public class RadioLink
{
    public int Id { get; set; }

    public int SpielId { get; set; }
    public Spiel Spiel { get; set; } = null!;

    /// <summary>Direkter Audiostream-Link fuer den Einzelspiel-Kommentar, falls vorhanden.</summary>
    public string? EinzelspielUrl { get; set; }

    /// <summary>Link zur Konferenzschaltung, falls dieses Spiel Teil der Konferenz ist.</summary>
    public string? KonferenzUrl { get; set; }

    public DateTimeOffset ErmitteltAm { get; set; }
}
