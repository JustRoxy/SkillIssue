using System.Text.Json.Serialization;
using SkillIssue.Common;
using SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts.Match.Events.Game;

namespace SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts.Match.Events;

public class MatchEvent : IMergable<IList<MatchEvent>>
{
    [JsonPropertyName("id")] public long EventId { get; set; }
    [JsonPropertyName("detail")] public MatchEventDetail Detail { get; set; } = new();
    [JsonPropertyName("timestamp")] public DateTimeOffset Timestamp { get; set; }
    [JsonPropertyName("user_id")] public int? UserId { get; set; }
    [JsonPropertyName("game")] public MatchGame? Game { get; set; }

    public static IList<MatchEvent> Merge(IList<MatchEvent> before, IList<MatchEvent> after)
    {
        //UnionBy works by constructing a hashset for keySelector. If we assume after has more accurate and up-to-date information we should use after.UnionBy, not before.UnionBy 
        return after.UnionBy(before, @event => @event.EventId).OrderBy(ev => ev.EventId).ToList();
    }
}