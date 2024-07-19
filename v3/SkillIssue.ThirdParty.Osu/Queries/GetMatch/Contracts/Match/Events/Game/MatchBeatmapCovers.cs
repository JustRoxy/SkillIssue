using System.Text.Json.Serialization;

namespace SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts.Match.Events.Game;

public class MatchBeatmapCovers
{
    [JsonPropertyName("cover")] public string Cover { get; set; }

    [JsonPropertyName("cover@2x")] public string Cover2x { get; set; }

    [JsonPropertyName("card")] public string Card { get; set; }

    [JsonPropertyName("card@2x")] public string Card2x { get; set; }

    [JsonPropertyName("list")] public string List { get; set; }

    [JsonPropertyName("list@2x")] public string List2x { get; set; }

    [JsonPropertyName("slimcover")] public string Slimcover { get; set; }

    [JsonPropertyName("slimcover@2x")] public string Slimcover2x { get; set; }
}