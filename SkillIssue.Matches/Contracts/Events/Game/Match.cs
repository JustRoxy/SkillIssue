using System.Text.Json.Serialization;

namespace SkillIssue.Matches.Contracts.Events.Game;

public class Match
{
    [JsonPropertyName("slot")] public int Slot { get; set; }

    [JsonPropertyName("team")] public string Team { get; set; }

    [JsonPropertyName("pass")] public bool Pass { get; set; }
}