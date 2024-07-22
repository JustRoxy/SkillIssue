using System.Text.Json.Serialization;

namespace SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts.Match.Events;

public class MatchEventDetail
{
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
}