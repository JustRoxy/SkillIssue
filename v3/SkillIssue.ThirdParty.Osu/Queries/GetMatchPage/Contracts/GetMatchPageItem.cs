using System.Text.Json.Serialization;

namespace SkillIssue.ThirdParty.Osu.Queries.GetMatchPage.Contracts;

public class GetMatchPageItem
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("start_time")] public DateTime StartTime { get; set; }
    [JsonPropertyName("end_time")] public DateTime? EndTime { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = null!;
}