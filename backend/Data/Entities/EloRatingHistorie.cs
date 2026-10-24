namespace BetQuote.Api.Data.Entities;

/// <summary>
/// Zeitreihe des selbst berechneten Elo-Ratings pro Team (PREDICTION_FACTORS.md Abschnitt 1:
/// "ein einzelner, laufend nach jedem Spiel aktualisierter Staerkewert je Team"). Ein Eintrag pro
/// Team UND pro Spiel, an dem das Team beteiligt war - keine reine "aktueller Wert"-Spalte auf
/// Team, weil das Modell (Poisson/Dixon-Coles + Gradient Boosting) den Elo-Wert VOR einem
/// bestimmten Spiel als Feature braucht, nicht nur den aktuellsten Stand.
/// </summary>
public class EloRatingHistorie
{
    public int Id { get; set; }

    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    /// <summary>Spiel, nach dessen Ergebnis dieser Elo-Wert berechnet wurde.</summary>
    public int SpielId { get; set; }
    public Spiel Spiel { get; set; } = null!;

    /// <summary>Elo-Wert des Teams VOR diesem Spiel (Modell-Input fuer die Vorhersage dieses Spiels).</summary>
    public decimal EloVorSpiel { get; set; }

    /// <summary>Elo-Wert des Teams NACH diesem Spiel (Ergebnis der Elo-Aktualisierung).</summary>
    public decimal EloNachSpiel { get; set; }

    /// <summary>Zeitpunkt des Spiels - denormalisiert fuer schnelle chronologische Sortierung ohne Join auf Spiel.Anstoss.</summary>
    public DateTimeOffset SpielZeitpunkt { get; set; }
}
