namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Ein Fussball-Verein/Team, quellenuebergreifend ueber eine eigene ID identifiziert. Die
/// unterschiedliche Schreibweise je Datenquelle ("Bayern Muenchen" vs. "FC Bayern Muenchen" vs.
/// "Bayern") wird ueber <see cref="TeamAlias"/> aufgeloest, siehe PREDICTION_FACTORS.md
/// "Team-/Spiel-Mapping".
/// </summary>
public class Team
{
    public int Id { get; set; }

    /// <summary>Kanonischer Anzeigename, z.B. "FC Bayern München".</summary>
    public required string Name { get; set; }

    /// <summary>Kurzname fuer kompakte Anzeigen (Tabellen, Badges), z.B. "Bayern".</summary>
    public string? Kurzname { get; set; }

    /// <summary>Team-Logo-URL, direkt von OpenLigaDB uebernommen (teamIconUrl), siehe APP_STRUCTURE.md.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>OpenLigaDB-Team-Id - Primaerquelle, direkter Fremdschluessel-Ersatz fuer Imports.</summary>
    public int? OpenLigaDbTeamId { get; set; }

    /// <summary>Vereinsstandort (Stadt) fuer die geplante Distanz-/Derby-Kennzahl, PREDICTION_FACTORS.md Abschnitt 5.</summary>
    public string? StandortStadt { get; set; }

    /// <summary>Breitengrad des Stadions/Vereinsstandorts (optional, fuer Distanzberechnung).</summary>
    public decimal? StandortBreitengrad { get; set; }

    /// <summary>Laengengrad des Stadions/Vereinsstandorts (optional, fuer Distanzberechnung).</summary>
    public decimal? StandortLaengengrad { get; set; }

    public ICollection<TeamAlias> Aliase { get; set; } = new List<TeamAlias>();
    public ICollection<Spieler> Spieler { get; set; } = new List<Spieler>();
    public ICollection<EloRatingHistorie> EloHistorie { get; set; } = new List<EloRatingHistorie>();
}
