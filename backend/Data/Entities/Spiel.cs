namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Ein einzelnes Spiel. Traegt sowohl die Spieltag-Nummer (pro Wettbewerb unterschiedlich
/// gezaehlt) als auch eine redundant gespeicherte ISO-Kalenderwoche/-Jahr-Zuordnung, weil der
/// Wochen-Feed (APP_STRUCTURE.md Abschnitt 1) explizit NICHT nach Spieltag, sondern nach
/// Kalenderwoche navigiert ("eine Kalenderwoche buendelt das natuerlicher als ein
/// wettbewerbsabhaengiger 'Spieltag'-Begriff"). KwJahr+KwNummer sind aus Anstoss redundant
/// abgeleitet (ISO-8601, siehe SpielConfiguration/Import) und als eigene Spalten gespeichert statt
/// nur zur Laufzeit berechnet, damit die Wochen-Feed-Abfrage ("alle Spiele einer KW ueber alle 4
/// Wettbewerbe") einen einfachen Index-Scan nutzen kann statt bei jeder Anfrage
/// EXTRACT(ISOYEAR/WEEK FROM ...) ueber die gesamte Tabelle laufen zu lassen.
/// </summary>
public class Spiel
{
    public int Id { get; set; }

    public int WettbewerbId { get; set; }
    public Wettbewerb Wettbewerb { get; set; } = null!;

    public int SaisonId { get; set; }
    public Saison Saison { get; set; } = null!;

    /// <summary>Spieltag-Nummer innerhalb der Saison/des Wettbewerbs. Bei Pokal-Runden hilfsweise die Rundennummer (1=1.Runde usw.).</summary>
    public int SpieltagNummer { get; set; }

    /// <summary>Anzeigename des Spieltags/der Runde, z.B. "18. Spieltag" oder "Achtelfinale" (aus OpenLigaDB group.groupName).</summary>
    public string? SpieltagName { get; set; }

    /// <summary>ISO-8601-Kalenderjahr des Anstosstermins (kann von Anstoss.Year an Jahresgrenzen abweichen, z.B. 30.12. in KW 1 des Folgejahres).</summary>
    public int KwJahr { get; set; }

    /// <summary>ISO-8601-Kalenderwochennummer (1-53) des Anstosstermins - Basis fuer die Wochen-Feed-Navigation.</summary>
    public int KwNummer { get; set; }

    public int HeimTeamId { get; set; }
    public Team HeimTeam { get; set; } = null!;

    public int AuswaertsTeamId { get; set; }
    public Team AuswaertsTeam { get; set; } = null!;

    /// <summary>Anstosszeitpunkt (UTC gespeichert, siehe DbContext-Konfiguration).</summary>
    public DateTimeOffset Anstoss { get; set; }

    public SpielStatus Status { get; set; } = SpielStatus.Geplant;

    /// <summary>Aktuelle Spielminute waehrend Status=Live (aus OpenLigaDB-Live-Polling), null wenn nicht live.</summary>
    public int? LiveMinute { get; set; }

    /// <summary>Laufender/Halbzeit-/End-Torstand Heimteam - bei Status=Geplant null ("– : –" in der UI, siehe APP_STRUCTURE.md).</summary>
    public int? ToreHeim { get; set; }

    /// <summary>Laufender/Halbzeit-/End-Torstand Auswaertsteam - bei Status=Geplant null.</summary>
    public int? ToreAuswaerts { get; set; }

    /// <summary>Halbzeit-Torstand Heimteam (fuer Erste-/Zweite-Halbzeit-Vergleich, PREDICTION_FACTORS.md Abschnitt 10).</summary>
    public int? HalbzeitToreHeim { get; set; }

    /// <summary>Halbzeit-Torstand Auswaertsteam.</summary>
    public int? HalbzeitToreAuswaerts { get; set; }

    public int? SchiedsrichterId { get; set; }
    public Schiedsrichter? Schiedsrichter { get; set; }

    /// <summary>OpenLigaDB-Match-Id - Primaerquelle fuer die deutschen Wettbewerbe (bl1/bl2), direkter Referenzschluessel fuer Re-Imports/Live-Updates.</summary>
    public long? OpenLigaDbMatchId { get; set; }

    /// <summary>football-data.org-Match-Id - Primaerquelle fuer La Liga (siehe PREDICTION_FACTORS.md "Ziel-Wettbewerbe"), analog zu OpenLigaDbMatchId fuer die deutschen Wettbewerbe.</summary>
    public long? FootballDataOrgMatchId { get; set; }

    public ICollection<TorEvent> TorEvents { get; set; } = new List<TorEvent>();
    public ICollection<Vorhersage> Vorhersagen { get; set; } = new List<Vorhersage>();

    /// <summary>Bis zu zwei Zeilen pro Team x Quelle (siehe XgStatistik) - Filterung nach Team/IstHeimteam erfolgt in der Abfrage, nicht ueber eine eigene 1:1-Navigation.</summary>
    public ICollection<XgStatistik> XgStatistiken { get; set; } = new List<XgStatistik>();

    public ICollection<ZeitsegmentStatistik> Zeitsegmente { get; set; } = new List<ZeitsegmentStatistik>();
    public RadioLink? RadioLink { get; set; }
    public ICollection<SpielerVerletzung> BetroffeneVerletzungen { get; set; } = new List<SpielerVerletzung>();
}
