using System.Net.Http.Json;

namespace BetQuote.Api.Services.OpenLigaDb;

public interface IOpenLigaDbClient
{
    /// <summary>Alle bei OpenLigaDB bekannten Liga/Saison-Kombinationen (fuer die Ermittlung der verfuegbaren Saisons je Wettbewerb).</summary>
    Task<List<OpenLigaDbLeagueDto>> GetAvailableLeaguesAsync(CancellationToken ct = default);

    /// <summary>Alle Spiele einer kompletten Saison eines Wettbewerbs (OpenLigaDB liefert bei diesem Endpoint die gesamte Saison auf einmal, nicht nur einen Spieltag).</summary>
    Task<List<OpenLigaDbMatchDto>> GetMatchesForSeasonAsync(string leagueShortcut, string season, CancellationToken ct = default);

    /// <summary>Aktuell laufende/anstehende Spiele eines Wettbewerbs (fuer haeufigeres Live-Polling) - liefert einen kleineren, aktuellen Ausschnitt statt der ganzen Saison.</summary>
    Task<List<OpenLigaDbMatchDto>> GetCurrentMatchdayMatchesAsync(string leagueShortcut, CancellationToken ct = default);
}

/// <summary>
/// Duenner Wrapper um https://api.openligadb.de (siehe PREDICTION_FACTORS.md "Datenquellen" /
/// TECH_STACK.md - Primaerquelle, kostenlos, keine Auth, kein bekanntes Rate-Limit). Bewusst kein
/// Retry/Circuit-Breaker hier eingebaut - Fehlerbehandlung (ein fehlgeschlagener Request darf den
/// Gesamtimport nicht abbrechen) passiert bewusst eine Ebene hoeher im <see cref="OpenLigaDbImportService"/>,
/// damit hier ein einzelner HTTP-Fehler einfach als Exception durchgereicht wird und der Aufrufer
/// entscheidet, wie granular er das behandelt (pro Liga/Saison statt pro einzelnem Request).
/// </summary>
public class OpenLigaDbClient(HttpClient httpClient) : IOpenLigaDbClient
{
    public async Task<List<OpenLigaDbLeagueDto>> GetAvailableLeaguesAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<OpenLigaDbLeagueDto>>("getavailableleagues", ct);
        return result ?? [];
    }

    public async Task<List<OpenLigaDbMatchDto>> GetMatchesForSeasonAsync(string leagueShortcut, string season, CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<OpenLigaDbMatchDto>>(
            $"getmatchdata/{Uri.EscapeDataString(leagueShortcut)}/{Uri.EscapeDataString(season)}", ct);
        return result ?? [];
    }

    public async Task<List<OpenLigaDbMatchDto>> GetCurrentMatchdayMatchesAsync(string leagueShortcut, CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<OpenLigaDbMatchDto>>(
            $"getmatchdata/{Uri.EscapeDataString(leagueShortcut)}", ct);
        return result ?? [];
    }
}
