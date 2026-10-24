namespace BetQuote.Api.Services.RadioLink;

/// <summary>
/// Ergebnis eines geparsten Audio-Eintrags von der Sportschau-Audio-Uebersichtsseite - siehe
/// SportschauScraperService fuer die Herkunft (data-v-JSON-Attribut jedes Audioplayer-Blocks).
/// </summary>
public record SportschauAudioEintrag(
    string Titel,
    DateTimeOffset? Anstoss,
    string Url,
    bool IstKonferenz);
