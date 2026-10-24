namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Alternativer Name desselben Teams in einer bestimmten Datenquelle (z.B. Understat schreibt
/// "Bayern Munich", FBref "Bayern Munich", OpenLigaDB "FC Bayern München"). Loest das
/// Multi-Quellen-Mapping-Problem aus PREDICTION_FACTORS.md ("Team-/Spiel-Mapping") auf: beim
/// Import wird zuerst nach (DatenquelleId, AliasName) gesucht, Datum+Team-Paarung dient laut
/// Fachvorgabe als Fallback-Schluessel dort, wo (noch) kein Alias hinterlegt ist.
/// </summary>
public class TeamAlias
{
    public int Id { get; set; }

    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public int DatenquelleId { get; set; }
    public Datenquelle Datenquelle { get; set; } = null!;

    /// <summary>Name/Schreibweise, wie die Quelle das Team tatsaechlich bezeichnet.</summary>
    public required string AliasName { get; set; }

    /// <summary>Optionale externe ID der Quelle (z.B. Sofascore-Team-Id), falls vorhanden - stabiler als der Name.</summary>
    public string? ExterneId { get; set; }
}
