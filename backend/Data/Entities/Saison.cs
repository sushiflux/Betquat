namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Eine Saison innerhalb eines Wettbewerbs (z.B. "2025/26"). Jeder Wettbewerb startet seine
/// Datenhistorie am jeweils fruehesten verlaesslich verfuegbaren Punkt - bewusst kein
/// gemeinsames Startjahr ueber alle vier Wettbewerbe hinweg (siehe PREDICTION_FACTORS.md
/// "Wichtige Einschraenkung, die wir bewusst akzeptieren").
/// </summary>
public class Saison
{
    public int Id { get; set; }

    public int WettbewerbId { get; set; }
    public Wettbewerb Wettbewerb { get; set; } = null!;

    /// <summary>Anzeigename, z.B. "2025/26" oder bei Kalenderjahr-Saisons wie 3.Liga ggf. "2025".</summary>
    public required string Name { get; set; }

    /// <summary>Startjahr der Saison (fuer Sortierung/Filter unabhaengig vom Anzeigeformat).</summary>
    public int Startjahr { get; set; }

    /// <summary>OpenLigaDB-Saison-Kuerzel, z.B. "2025" - Teil des API-Pfads bei OpenLigaDB.</summary>
    public string? OpenLigaDbSaisonKuerzel { get; set; }

    public bool IstAktuell { get; set; }

    public ICollection<Spiel> Spiele { get; set; } = new List<Spiel>();
}
