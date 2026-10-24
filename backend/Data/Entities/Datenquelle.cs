namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Feste Liste der externen Datenquellen (siehe TECH_STACK.md "Externe Datenquellen" und
/// PREDICTION_FACTORS.md "Datenquellen"). Als Lookup-Tabelle statt Enum, damit
/// <see cref="TeamAlias"/> und optionale Zusatzdaten (xG, Ballbesitz-Segmente) sauber
/// referenzieren koennen, welche Quelle den jeweiligen Wert geliefert hat - wichtig, weil mehrere
/// Quellen fuer denselben Kennwert existieren koennen (z.B. xG von Understat UND FBref).
/// </summary>
public class Datenquelle
{
    public int Id { get; set; }

    /// <summary>Eindeutiger technischer Schluessel, z.B. "openligadb", "understat", "sofascore", "fbref", "kaggle".</summary>
    public required string Schluessel { get; set; }

    public required string Anzeigename { get; set; }
}
