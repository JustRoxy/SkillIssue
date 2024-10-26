using System.Text.Json.Serialization;

namespace SkillIssue.Matches.Contracts.Events.Game;

public class MatchScore
{
    [JsonPropertyName("accuracy")] public double Accuracy { get; set; }

    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("max_combo")] public int MaxCombo { get; set; }

    [JsonPropertyName("mode")] public string Mode { get; set; } = "";

    [JsonPropertyName("mode_int")] public int ModeInt { get; set; }

    [JsonPropertyName("mods")] public List<string> Mods { get; set; } = [];

    [JsonPropertyName("passed")] public bool Passed { get; set; }

    [JsonPropertyName("perfect")] public int Perfect { get; set; }

    [JsonPropertyName("rank")] public string Rank { get; set; } = "";

    [JsonPropertyName("replay")] public bool Replay { get; set; }

    [JsonPropertyName("score")] public int TotalScore { get; set; }

    [JsonPropertyName("statistics")] public MatchScoreStatistics Statistics { get; set; } = new();

    [JsonPropertyName("type")] public string Type { get; set; } = "";

    [JsonPropertyName("user_id")] public int UserId { get; set; }

    [JsonPropertyName("match")] public Match Match { get; set; } = new();
}