using System.Text.Json.Serialization;

namespace SkillIssue.Matches.Contracts;

public class MatchInfo
{
    [JsonPropertyName("id")] public int MatchId { get; set; }
    [JsonPropertyName("start_time")] public DateTimeOffset StartTime { get; set; }
    [JsonPropertyName("end_time")] public DateTimeOffset? EndTime { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    //New data is always more accurate
    public static MatchInfo Merge(MatchInfo before, MatchInfo after)
    {
        return after;
    }
}