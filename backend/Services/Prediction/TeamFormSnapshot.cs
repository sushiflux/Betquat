namespace BetQuote.Api.Services.Prediction;

/// <summary>
/// Aggregierte Form-Kennzahlen eines Teams zu einem Stichzeitpunkt (vor einem bestimmten Spiel) -
/// Grundlage sowohl fuer die Poisson-Lambda-Berechnung als auch fuer den FaktorenSnapshot
/// (PREDICTION_FACTORS.md Abschnitt 1: "letzte 5-10 Spiele", "geschossene/kassierte Tore").
/// </summary>
public record TeamFormSnapshot(
    int TeamId,
    int AnzahlSpieleBetrachtet,
    decimal DurchschnittGeschosseneTore,
    decimal DurchschnittKassierteTore,
    decimal DurchschnittGeschosseneToreHeimOderAuswaerts,
    decimal DurchschnittKassierteToreHeimOderAuswaerts,
    decimal EloRating,
    int Tabellenposition,
    int PunkteLetzte5,
    IReadOnlyList<string> FormkurveLetzte5 // "S"/"U"/"N" je Spiel, juengstes zuerst
);
