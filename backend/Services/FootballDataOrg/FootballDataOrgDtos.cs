using System.Text.Json.Serialization;

namespace BetQuote.Api.Services.FootballDataOrg;

/// <summary>
/// DTOs fuer https://api.football-data.org/v4 - bewusst 1:1 an das externe JSON-Schema angelehnt
/// (siehe offizielle Doku unter https://docs.football-data.org/general/v4/), analog zum
/// OpenLigaDb-DTO-Ansatz (OpenLigaDbDtos.cs). Feld-Auswahl beschraenkt auf das, was der Importer
/// tatsaechlich braucht (Ergebnisse/Status/Teams/Spieltag) - football-data.org liefert u.a. auch
/// Schiedsrichter/Venue-Daten, die hier (noch) nicht gebraucht werden.
/// </summary>
public class FootballDataOrgMatchesResponseDto
{
    [JsonPropertyName("matches")]
    public List<FootballDataOrgMatchDto> Matches { get; set; } = [];
}

public class FootballDataOrgMatchDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("utcDate")]
    public DateTimeOffset UtcDate { get; set; }

    /// <summary>SCHEDULED, TIMED, IN_PLAY, PAUSED, FINISHED, POSTPONED, SUSPENDED, CANCELLED.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("matchday")]
    public int? Matchday { get; set; }

    [JsonPropertyName("season")]
    public FootballDataOrgSeasonDto? Season { get; set; }

    [JsonPropertyName("homeTeam")]
    public FootballDataOrgTeamDto HomeTeam { get; set; } = new();

    [JsonPropertyName("awayTeam")]
    public FootballDataOrgTeamDto AwayTeam { get; set; } = new();

    [JsonPropertyName("score")]
    public FootballDataOrgScoreDto Score { get; set; } = new();

    [JsonPropertyName("minute")]
    public int? Minute { get; set; }
}

public class FootballDataOrgSeasonDto
{
    [JsonPropertyName("startDate")]
    public DateOnly? StartDate { get; set; }
}

public class FootballDataOrgTeamDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("shortName")]
    public string? ShortName { get; set; }

    [JsonPropertyName("crest")]
    public string? Crest { get; set; }
}

public class FootballDataOrgScoreDto
{
    [JsonPropertyName("fullTime")]
    public FootballDataOrgScorePairDto FullTime { get; set; } = new();

    [JsonPropertyName("halfTime")]
    public FootballDataOrgScorePairDto HalfTime { get; set; } = new();
}

public class FootballDataOrgScorePairDto
{
    [JsonPropertyName("home")]
    public int? Home { get; set; }

    [JsonPropertyName("away")]
    public int? Away { get; set; }
}
