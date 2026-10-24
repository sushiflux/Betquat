namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Verletzungs- oder Sperr-Zeitraum eines Spielers (PREDICTION_FACTORS.md Abschnitt 4:
/// "Verletzungen/Sperren von Schluesselspielern"). Ein Zeitraum statt nur ein Flag, damit sich
/// sowohl "aktuell verletzt" als auch "Rueckkehrer der letzten 2-3 Spiele" (eigener Faktor in
/// Abschnitt 4) daraus ableiten lassen.
/// </summary>
public class SpielerVerletzung
{
    public int Id { get; set; }

    public int SpielerId { get; set; }
    public Spieler Spieler { get; set; } = null!;

    public SpielerVerletzungsArt Art { get; set; }

    /// <summary>Freitext-Grund, z.B. "Muskelfaserriss" oder "Gelb-Rote Karte" - optional, fuer die Begruendungs-Anzeige (APP_STRUCTURE.md Spiel-Detail).</summary>
    public string? Grund { get; set; }

    public DateOnly Von { get; set; }

    /// <summary>Bis-Datum, wenn bekannt/prognostiziert - null wenn noch offen ("bis auf Weiteres").</summary>
    public DateOnly? Bis { get; set; }

    /// <summary>Optionaler Verweis auf ein konkretes Spiel, falls die Verletzung/Sperre direkt aus einem Spiel-Event resultiert (z.B. Rote Karte -&gt; Sperre).</summary>
    public int? AusloesendesSpielId { get; set; }
    public Spiel? AusloesendesSpiel { get; set; }
}

public enum SpielerVerletzungsArt
{
    Verletzung = 0,
    Sperre = 1,
}
