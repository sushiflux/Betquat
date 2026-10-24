namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Ein einzelnes Tor innerhalb eines Spiels, inkl. Minute und Elfmeter-/Eigentor-Kennzeichnung
/// (direkt aus OpenLigaDB uebernehmbar, siehe APP_STRUCTURE.md "Torschuetzen mit Minute,
/// Elfmeter-/Eigentor-Kennzeichnung"). Basis fuer die 15-Minuten-Segment-Auswertung aus
/// PREDICTION_FACTORS.md Abschnitt 10 ("aus Tor-Minuten ableitbar ... keine Zusatzquelle noetig").
/// </summary>
public class TorEvent
{
    public int Id { get; set; }

    public int SpielId { get; set; }
    public Spiel Spiel { get; set; } = null!;

    /// <summary>Team, das dieses Tor erzielt hat (bei Eigentor: das Team, dem das Tor angerechnet wird, nicht der Schuetzenverein).</summary>
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    /// <summary>Torschuetze - nullable, weil OpenLigaDB nicht immer einen Spielernamen mitliefert (v.a. 3. Liga historisch).</summary>
    public int? SpielerId { get; set; }
    public Spieler? Spieler { get; set; }

    /// <summary>Freitext-Schuetzenname als Fallback, falls kein Spieler-Datensatz gematcht werden konnte.</summary>
    public string? SpielerNameFreitext { get; set; }

    /// <summary>Spielminute (kann bei Nachspielzeit &gt;45/&gt;90 sein).</summary>
    public int Minute { get; set; }

    public bool IstElfmeter { get; set; }
    public bool IstEigentor { get; set; }

    /// <summary>Spielstand nach diesem Tor, Heimteam - direkt aus OpenLigaDB uebernehmbar, spart Neuberechnung.</summary>
    public int ToreHeimNachEvent { get; set; }

    /// <summary>Spielstand nach diesem Tor, Auswaertsteam.</summary>
    public int ToreAuswaertsNachEvent { get; set; }
}
