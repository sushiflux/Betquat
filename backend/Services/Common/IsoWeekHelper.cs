using System.Globalization;

namespace BetQuote.Api.Services.Common;

/// <summary>
/// Berechnet ISO-8601-Kalenderjahr/-woche aus einem Zeitpunkt - Basis fuer Spiel.KwJahr/KwNummer
/// (siehe Spiel.cs: "aus Anstoss redundant abgeleitet (ISO-8601)"). .NET's eingebautes
/// ISOWeek nutzt bereits ISO-8601-Regeln (Woche 1 = die Woche mit dem ersten Donnerstag des
/// Jahres, Montag als Wochenstart) - kein eigener Algorithmus noetig.
/// </summary>
public static class IsoWeekHelper
{
    public static (int Jahr, int Woche) GetIsoYearAndWeek(DateTimeOffset zeitpunkt)
    {
        // ISOWeek arbeitet auf lokalem DateTime - Anstosszeiten werden UTC gespeichert, die
        // Kalenderwoche soll sich aber auf die tatsaechliche (europaeische) Anstosszeit beziehen,
        // nicht auf UTC (relevant nahe Mitternacht/Jahreswechsel). Deutschland: CET/CEST.
        var berlinZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        var lokal = TimeZoneInfo.ConvertTime(zeitpunkt, berlinZone).DateTime;

        var jahr = ISOWeek.GetYear(lokal);
        var woche = ISOWeek.GetWeekOfYear(lokal);
        return (jahr, woche);
    }
}
