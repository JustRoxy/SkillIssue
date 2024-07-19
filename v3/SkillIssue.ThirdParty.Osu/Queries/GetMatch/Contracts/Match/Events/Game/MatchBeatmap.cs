using System.Text.Json.Serialization;

namespace SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts.Match.Events.Game;

public class MatchBeatmap
{
    [JsonPropertyName("beatmapset_id")] public int BeatmapsetId { get; set; }

    [JsonPropertyName("difficulty_rating")]
    public double DifficultyRating { get; set; }

    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("mode")] public string Mode { get; set; }

    [JsonPropertyName("status")] public string Status { get; set; }

    [JsonPropertyName("total_length")] public int TotalLength { get; set; }

    [JsonPropertyName("user_id")] public int UserId { get; set; }

    [JsonPropertyName("version")] public string Version { get; set; }

    [JsonPropertyName("beatmapset")] public MatchBeatmapset Beatmapset { get; set; }
}