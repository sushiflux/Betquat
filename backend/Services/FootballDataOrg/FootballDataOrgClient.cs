using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BetQuote.Api.Services.FootballDataOrg;

public interface IFootballDataOrgClient
{
    /// <summary>True, wenn ein API-Key konfiguriert ist (siehe FOOTBALL_DATA_ORG_API_KEY) - ohne Key kann dieser Client keine Requests stellen, siehe Klassendoku.</summary>
    bool IstKonfiguriert { get; }

    /// <summary>Alle Spiele eines Wettbewerbs fuer eine Saison (Startjahr, z.B. 2025 fuer Saison 2025/26) - football-data.org liefert hier bereits die komplette Saison in einer Antwort, analog zu OpenLigaDB.</summary>
    Task<List<FootballDataOrgMatchDto>> GetMatchesForSeasonAsync(string competitionCode, int saisonStartjahr, CancellationToken ct = default);

    /// <summary>Aktuell laufende/kommende Spiele eines Wettbewerbs (kein Saison-Filter) - fuer haeufigeres Live-Polling deutlich billiger als eine volle Saisonabfrage.</summary>
    Task<List<FootballDataOrgMatchDto>> GetCurrentMatchesAsync(string competitionCode, CancellationToken ct = default);
}

/// <summary>
/// Duenner Wrapper um https://api.football-data.org/v4 (siehe PREDICTION_FACTORS.md/TECH_STACK.md
/// "Datenquellen" - Primaerquelle fuer La Liga, da OpenLigaDB dort nur ein instabiles
/// Community-Schema fuehrt). Erfordert einen kostenlosen API-Key (Registrierung unter
/// https://www.football-data.org/client/register), der per X-Auth-Token-Header mitgeschickt wird -
/// gelesen aus der Umgebungsvariable FOOTBALL_DATA_ORG_API_KEY (siehe Program.cs/.env.example).
///
/// OFFENER PUNKT (siehe Abschlussbericht): dieser Key kann nicht automatisiert durch den Agenten
/// registriert werden (erfordert eine echte E-Mail-Adresse/menschliche Registrierung) - der
/// Nutzer muss ihn manuell besorgen und in der .env/im Secret hinterlegen. Ohne Key liefert
/// <see cref="IstKonfiguriert"/> false und der zugehoerige Importer (FootballDataOrgImportService)
/// ueberspringt La Liga mit einer klaren Log-Meldung statt zu werfen - kein Blocker fuer den Rest
/// der App (gleiches Fehlerhandlungsprinzip wie beim OpenLigaDB-Importer).
///
/// Rate-Limit (kostenloser Tier): 10 Requests/Minute - deutlich enger als OpenLigaDB. Der Importer
/// haelt sich daran, indem er Saisons sequenziell mit einer kleinen Pause zwischen Requests abruft
/// (siehe FootballDataOrgImportService).
/// </summary>
public class FootballDataOrgClient(HttpClient httpClient, IConfiguration configuration, ILogger<FootballDataOrgClient> logger) : IFootballDataOrgClient
{
    private readonly string? _apiKey = configuration["FOOTBALL_DATA_ORG_API_KEY"];

    public bool IstKonfiguriert => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<List<FootballDataOrgMatchDto>> GetMatchesForSeasonAsync(string competitionCode, int saisonStartjahr, CancellationToken ct = default)
    {
        var response = await SendeAsync($"competitions/{competitionCode}/matches?season={saisonStartjahr}", ct);
        return response?.Matches ?? [];
    }

    public async Task<List<FootballDataOrgMatchDto>> GetCurrentMatchesAsync(string competitionCode, CancellationToken ct = default)
    {
        var response = await SendeAsync($"competitions/{competitionCode}/matches", ct);
        return response?.Matches ?? [];
    }

    private async Task<FootballDataOrgMatchesResponseDto?> SendeAsync(string relativeUrl, CancellationToken ct)
    {
        if (!IstKonfiguriert)
        {
            logger.LogDebug("football-data.org-Request uebersprungen: kein API-Key konfiguriert (FOOTBALL_DATA_ORG_API_KEY)");
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        request.Headers.TryAddWithoutValidation("X-Auth-Token", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            // Rate-Limit (10 Req/Min im Free-Tier) ueberschritten - kein harter Fehler, der Import
            // faengt das eine Ebene hoeher ab und macht beim naechsten Zyklus weiter.
            logger.LogWarning("football-data.org Rate-Limit erreicht (429) fuer {Url}", relativeUrl);
            response.EnsureSuccessStatusCode();
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FootballDataOrgMatchesResponseDto>(cancellationToken: ct);
    }
}
