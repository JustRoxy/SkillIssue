using System.Text.Json.Serialization;

namespace SkillIssue.Matches.Contracts;

public class Page
{
    [JsonPropertyName("matches")]
    public List<MatchInfo> Matches { get; set; } = [];

    [JsonPropertyName("cursor")]
    public CursorContract Cursor { get; set; } = new();

    public class CursorContract
    {
        public int MatchId { get; set; }
    }
}