using System.Text.Json.Serialization;

namespace SkillIssue.ThirdParty.API.Osu.Queries.GetMatchPage.Contracts;

public class GetMatchPageResponse
{
    [JsonPropertyName("matches")] public List<GetMatchPageItem> Matches { get; set; } = [];
}