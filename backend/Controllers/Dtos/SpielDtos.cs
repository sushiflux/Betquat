using BetQuote.Api.Data.Entities;

namespace BetQuote.Api.Controllers.Dtos;

/// <summary>Kompakte Spiel-Repraesentation fuer den Wochen-Feed (APP_STRUCTURE.md Abschnitt 1) - eine Karte pro Spiel inkl. aktueller Vorhersage.</summary>
public record SpielListeDto(
    int Id,
    int WettbewerbId,
    string WettbewerbName,
    string WettbewerbKurzname,
    int SpieltagNummer,
    string? SpieltagName,
    DateTimeOffset Anstoss,
    int KwJahr,
    int KwNummer,
    TeamDto HeimTeam,
    TeamDto AuswaertsTeam,
    string Status,
    int? LiveMinute,
    int? ToreHeim,
    int? ToreAuswaerts,
    VorhersageKompaktDto? Vorhersage,
    bool HatRadioLink);

public record TeamDto(int Id, string Name, string? Kurzname, string? LogoUrl);

public record VorhersageKompaktDto(
    int TippToreHeim,
    int TippToreAuswaerts,
    decimal TippWahrscheinlichkeit,
    decimal WahrscheinlichkeitHeimsieg,
    decimal WahrscheinlichkeitRemis,
    decimal WahrscheinlichkeitAuswaertssieg,
    bool? TrafExakt,
    bool? TrafTendenz,
    bool IstHoheKonfidenz,
    decimal KonfidenzScore);

/// <summary>Ein Tag innerhalb des Wochen-Feeds mit seinen Spielen, chronologisch gruppiert (APP_STRUCTURE.md: "Innerhalb einer KW sind die Spiele nach Wochentag gruppiert").</summary>
public record WochenFeedTagDto(DateOnly Datum, List<SpielListeDto> Spiele);

public record WochenFeedDto(int KwJahr, int KwNummer, List<WochenFeedTagDto> Tage);

/// <summary>Vollstaendiges Spiel-Detail (APP_STRUCTURE.md Abschnitt 2).</summary>
public record SpielDetailDto(
    int Id,
    int WettbewerbId,
    string WettbewerbName,
    int SpieltagNummer,
    string? SpieltagName,
    DateTimeOffset Anstoss,
    TeamDto HeimTeam,
    TeamDto AuswaertsTeam,
    string Status,
    int? LiveMinute,
    int? ToreHeim,
    int? ToreAuswaerts,
    int? HalbzeitToreHeim,
    int? HalbzeitToreAuswaerts,
    List<TorEventDto> TorEvents,
    VorhersageDetailDto? Vorhersage,
    List<HeadToHeadEintragDto> HeadToHead,
    List<string> FormkurveHeim,
    List<string> FormkurveAuswaerts,
    RadioLinkDto? RadioLink);

public record TorEventDto(int TeamId, string? SpielerName, int Minute, bool IstElfmeter, bool IstEigentor, int ToreHeimNachEvent, int ToreAuswaertsNachEvent);

public record VorhersageDetailDto(
    int TippToreHeim,
    int TippToreAuswaerts,
    decimal TippWahrscheinlichkeit,
    decimal WahrscheinlichkeitHeimsieg,
    decimal WahrscheinlichkeitRemis,
    decimal WahrscheinlichkeitAuswaertssieg,
    decimal? WahrscheinlichkeitUeber25,
    decimal? WahrscheinlichkeitBeideTreffen,
    List<ErgebnisWahrscheinlichkeitDto> Verteilung,
    object FaktorenSnapshot,
    bool? TrafExakt,
    bool? TrafTendenz,
    DateTimeOffset BerechnetAm,
    bool IstHoheKonfidenz,
    decimal KonfidenzScore);

public record ErgebnisWahrscheinlichkeitDto(int Heim, int Auswaerts, decimal Wahrscheinlichkeit);

public record HeadToHeadEintragDto(DateTimeOffset Anstoss, string HeimTeamName, string AuswaertsTeamName, int? ToreHeim, int? ToreAuswaerts);

public record RadioLinkDto(string? EinzelspielUrl, string? KonferenzUrl);

public static class SpielDtoMapper
{
    public static TeamDto ToDto(this Team team) => new(team.Id, team.Name, team.Kurzname, team.LogoUrl);

    public static string ToStatusString(this SpielStatus status) => status switch
    {
        SpielStatus.Geplant => "geplant",
        SpielStatus.Live => "live",
        SpielStatus.Beendet => "beendet",
        SpielStatus.Abgesagt => "abgesagt",
        _ => "geplant",
    };

    /// <summary>
    /// Gemeinsame Spiel-&gt;Kartenliste-Projektion, genutzt vom Wochen-Feed (SpieleController) UND
    /// der Pokal-Rundenuebersicht (TabelleController) - dieselbe Karten-Darstellung eines Spiels
    /// soll unabhaengig vom aufrufenden Endpoint identisch aussehen. Erwartet, dass Wettbewerb/
    /// HeimTeam/AuswaertsTeam/RadioLink/Vorhersagen(IstAktuell) bereits eager geladen sind.
    /// </summary>
    public static SpielListeDto ToListeDto(this Spiel s)
    {
        var vorhersage = s.Vorhersagen.FirstOrDefault(v => v.IstAktuell);

        return new SpielListeDto(
            s.Id,
            s.WettbewerbId,
            s.Wettbewerb.Name,
            s.Wettbewerb.Kurzname,
            s.SpieltagNummer,
            s.SpieltagName,
            s.Anstoss,
            s.KwJahr,
            s.KwNummer,
            s.HeimTeam.ToDto(),
            s.AuswaertsTeam.ToDto(),
            s.Status.ToStatusString(),
            s.LiveMinute,
            s.ToreHeim,
            s.ToreAuswaerts,
            vorhersage is null ? null : new VorhersageKompaktDto(
                vorhersage.TippToreHeim,
                vorhersage.TippToreAuswaerts,
                vorhersage.TippWahrscheinlichkeit,
                vorhersage.WahrscheinlichkeitHeimsieg,
                vorhersage.WahrscheinlichkeitRemis,
                vorhersage.WahrscheinlichkeitAuswaertssieg,
                vorhersage.TrafExakt,
                vorhersage.TrafTendenz,
                vorhersage.IstHoheKonfidenz,
                vorhersage.KonfidenzScore),
            s.RadioLink is not null && (s.RadioLink.EinzelspielUrl != null || s.RadioLink.KonferenzUrl != null));
    }
}
