namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Einer der drei Ziel-Wettbewerbe (1. Bundesliga, 2. Bundesliga, La Liga), siehe
/// PREDICTION_FACTORS.md "Entscheidungen -> Ziel-Wettbewerbe" ("Aktualisiert (Nutzer-Entscheidung,
/// ersetzt die urspruengliche Wahl)": 3. Liga/DFB-Pokal wurden wieder rausgenommen, La Liga kam neu
/// dazu). Bewusst als eigene Tabelle statt Enum, weil Saisons, Spiele, Tabellen usw. per
/// Fremdschluessel daran haengen und pro Quelle ein eigenes externes Kuerzel mitgefuehrt werden muss
/// - zwei Quellen aktiv (siehe TECH_STACK.md "Externe Datenquellen"): OpenLigaDB fuer bl1/bl2,
/// football-data.org fuer La Liga (OpenLigaDB deckt La Liga nur ueber ein instabiles
/// Community-Schema ab, daher bewusst eine zweite Quelle statt Kompromiss bei der Datenqualitaet).
/// </summary>
public class Wettbewerb
{
    public int Id { get; set; }

    /// <summary>Anzeigename, z.B. "1. Bundesliga".</summary>
    public required string Name { get; set; }

    /// <summary>Kurzname fuer Badges/kompakte Anzeigen, z.B. "BL1" (siehe APP_STRUCTURE.md Wochen-Feed-Badges).</summary>
    public required string Kurzname { get; set; }

    /// <summary>Land des Wettbewerbs, z.B. "Deutschland"/"Spanien" - Basis fuer die Land-&gt;Liga-Tab-Navigation im Wochen-Feed (APP_STRUCTURE.md). Bewusst ein einfaches String-Feld statt einer eigenen Land-Tabelle - bei aktuell nur zwei Laendern waere eine Normalisierung reiner Overhead ohne Abfragevorteil (kein Land-spezifischer Zusatzwert wie Flagge-URL o.ae. geplant).</summary>
    public required string Land { get; set; }

    /// <summary>OpenLigaDB-Liga-Kuerzel (bl1, bl2) - fuer die deutschen Wettbewerbe, siehe PREDICTION_FACTORS.md. Null fuer Wettbewerbe, die stattdessen ueber football-data.org importiert werden (z.B. La Liga).</summary>
    public string? OpenLigaDbKuerzel { get; set; }

    /// <summary>football-data.org-Competition-Code (z.B. "PD" fuer La Liga/Primera Division) - siehe TECH_STACK.md. Null fuer Wettbewerbe, die ueber OpenLigaDB laufen.</summary>
    public string? FootballDataOrgCode { get; set; }

    /// <summary>Ligamodus (Tabelle) vs. Pokal-Turnierbaum - steuert Live-Tabellen-Widget (APP_STRUCTURE.md Abschnitt 1b). Aktuell laufen alle drei Ziel-Wettbewerbe im klassischen Ligamodus (kein Pokal mehr im Scope), das Feld bleibt fuer eine spaetere Wiederaufnahme eines Pokal-Wettbewerbs bestehen.</summary>
    public bool IstPokal { get; set; }

    /// <summary>
    /// True fuer die aktuell im Scope aktiven Ziel-Wettbewerbe (1./2. Bundesliga, La Liga), false
    /// fuer historisch in der DB belassene, aber nicht mehr aktiv importierte Wettbewerbe (3. Liga,
    /// DFB-Pokal - siehe ScopeAenderungLaLigaUndKonfidenz-Migration: bewusst nicht geloescht, um
    /// bereits importierte Spiele nicht zu verlieren). Explizites Flag statt impliziter Ableitung
    /// aus OpenLigaDbKuerzel/FootballDataOrgCode, da beide Felder bei den stillgelegten
    /// Wettbewerbe weiterhin gesetzt sind (nur der Importer nutzt sie nicht mehr aktiv) -
    /// steuert insbesondere, welche Wettbewerbe in der Land-&gt;Liga-Tab-Navigation des Wochen-
    /// Feeds auftauchen (WettbewerbeController).
    /// </summary>
    public bool IstAktiv { get; set; } = true;

    /// <summary>Anzeigereihenfolge in Badges/Filtern (1=1.BL, 2=2.BL, 3=La Liga).</summary>
    public int Sortierung { get; set; }

    public ICollection<Saison> Saisons { get; set; } = new List<Saison>();
}
