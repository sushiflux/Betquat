using System.Text.Json.Serialization;

namespace BetQuote.Api.Services.OpenLigaDb;

/// <summary>
/// DTOs fuer die Antwortformate von https://api.openligadb.de - bewusst 1:1 an das externe JSON-
/// Schema angelehnt (Feldnamen wie von der API geliefert, ueber JsonPropertyName gemappt), damit
/// diese Klassen ausschliesslich die Rohantwort abbilden. Die eigentliche Umsetzung ins interne
/// Datenmodell (Data/Entities) passiert im Importer, nicht hier.
/// </summary>
public class OpenLigaDbLeagueDto
{
    [JsonPropertyName("leagueId")]
    public int LeagueId { get; set; }

    [JsonPropertyName("leagueName")]
    public string LeagueName { get; set; } = "";

    [JsonPropertyName("leagueShortcut")]
    public string LeagueShortcut { get; set; } = "";

    /// <summary>Wird von der API teils als String, teils als Zahl geliefert - daher als String gemappt und beim Verbrauch geparst.</summary>
    [JsonPropertyName("leagueSeason")]
    public string? LeagueSeason { get; set; }
}

public class OpenLigaDbMatchDto
{
    [JsonPropertyName("matchID")]
    public long MatchId { get; set; }

    [JsonPropertyName("matchDateTimeUTC")]
    public DateTimeOffset? MatchDateTimeUtc { get; set; }

    [JsonPropertyName("leagueShortcut")]
    public string? LeagueShortcut { get; set; }

    [JsonPropertyName("leagueSeason")]
    public int? LeagueSeason { get; set; }

    [JsonPropertyName("group")]
    public OpenLigaDbGroupDto? Group { get; set; }

    [JsonPropertyName("team1")]
    public OpenLigaDbTeamDto Team1 { get; set; } = new();

    [JsonPropertyName("team2")]
    public OpenLigaDbTeamDto Team2 { get; set; } = new();

    [JsonPropertyName("matchIsFinished")]
    public bool MatchIsFinished { get; set; }

    [JsonPropertyName("matchResults")]
    public List<OpenLigaDbMatchResultDto> MatchResults { get; set; } = [];

    [JsonPropertyName("goals")]
    public List<OpenLigaDbGoalDto> Goals { get; set; } = [];

    [JsonPropertyName("lastUpdateDateTime")]
    public DateTimeOffset? LastUpdateDateTime { get; set; }
}

public class OpenLigaDbGroupDto
{
    [JsonPropertyName("groupName")]
    public string? GroupName { get; set; }

    [JsonPropertyName("groupOrderID")]
    public int GroupOrderId { get; set; }
}

public class OpenLigaDbTeamDto
{
    [JsonPropertyName("teamId")]
    public int TeamId { get; set; }

    [JsonPropertyName("teamName")]
    public string TeamName { get; set; } = "";

    [JsonPropertyName("shortName")]
    public string? ShortName { get; set; }

    [JsonPropertyName("teamIconUrl")]
    public string? TeamIconUrl { get; set; }
}

public class OpenLigaDbMatchResultDto
{
    [JsonPropertyName("pointsTeam1")]
    public int PointsTeam1 { get; set; }

    [JsonPropertyName("pointsTeam2")]
    public int PointsTeam2 { get; set; }

    /// <summary>"HalfTime" oder "After90Minutes" (Endergebnis) - bei Verlaengerung/Elfmeterschiessen im Pokal ggf. weitere Kinds, wir werten nur die beiden bekannten aus.</summary>
    [JsonPropertyName("resultTypeKind")]
    public string? ResultTypeKind { get; set; }
}

public class OpenLigaDbGoalDto
{
    [JsonPropertyName("goalID")]
    public long GoalId { get; set; }

    // Nullable, obwohl fachlich immer ein Zwischenstand: OpenLigaDB liefert bei manchen aelteren/
    // fehlerhaften Datensaetzen tatsaechlich null statt einer Zahl (beobachtet u.a. bl1 Saison
    // 2010) - robust parsen statt beim Import abzubrechen (Aufgabenstellung: "ein einzelner
    // fehlgeschlagener Request darf nicht den ganzen Importlauf abbrechen").
    [JsonPropertyName("scoreTeam1")]
    public int? ScoreTeam1 { get; set; }

    [JsonPropertyName("scoreTeam2")]
    public int? ScoreTeam2 { get; set; }

    [JsonPropertyName("matchMinute")]
    public int? MatchMinute { get; set; }

    [JsonPropertyName("goalGetterName")]
    public string? GoalGetterName { get; set; }

    [JsonPropertyName("scoringTeamId")]
    public int? ScoringTeamId { get; set; }

    [JsonPropertyName("isPenalty")]
    public bool IsPenalty { get; set; }

    [JsonPropertyName("isOwnGoal")]
    public bool IsOwnGoal { get; set; }
}
