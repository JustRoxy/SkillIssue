using System.Text.Json.Serialization;

namespace SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts.Match.Events.Game;

public class Match
{
    [JsonPropertyName("slot")] public int Slot { get; set; }

    [JsonPropertyName("team")] public string Team { get; set; }

    [JsonPropertyName("pass")] public bool Pass { get; set; }
}