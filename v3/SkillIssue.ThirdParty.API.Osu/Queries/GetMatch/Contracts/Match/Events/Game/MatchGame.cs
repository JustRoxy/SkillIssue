using System.Text.Json.Serialization;

namespace SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts.Match.Events.Game;

public class MatchGame
{
    [JsonPropertyName("beatmap_id")] public int BeatmapId { get; set; }

    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("start_time")] public DateTimeOffset StartTime { get; set; }

    [JsonPropertyName("end_time")] public DateTimeOffset? EndTime { get; set; }

    [JsonPropertyName("mode")] public string Mode { get; set; }

    [JsonPropertyName("mode_int")] public int ModeInt { get; set; }

    [JsonPropertyName("scoring_type")] public string ScoringType { get; set; }

    [JsonPropertyName("team_type")] public string TeamType { get; set; }

    [JsonPropertyName("mods")] public List<string> Mods { get; set; } = [];

    [JsonPropertyName("beatmap")] public MatchBeatmap Beatmap { get; set; }

    [JsonPropertyName("scores")] public List<MatchScore> Scores { get; set; } = [];
}