using System.Text.Json.Serialization;
using SkillIssue.Matches.Contracts.Events;

namespace SkillIssue.Matches.Contracts;

public class MatchResponse
{
    [JsonPropertyName("match")] public MatchInfo MatchInfo { get; init; } = new();
    [JsonPropertyName("first_event_id")] public long FirstEventId { get; init; }
    [JsonPropertyName("latest_event_id")] public long LatestEventId { get; init; }
    [JsonPropertyName("current_game_id")] public long? CurrentGameId { get; init; }
    [JsonPropertyName("events")] public IList<MatchEvent> Events { get; init; } = [];
    [JsonPropertyName("users")] public IList<MatchUser> Users { get; init; } = [];

    public static MatchResponse Merge(MatchResponse before, MatchResponse after)
    {
        return new MatchResponse
        {
            CurrentGameId = after.CurrentGameId,
            FirstEventId = after.FirstEventId,
            LatestEventId = after.LatestEventId,
            MatchInfo = MatchInfo.Merge(before.MatchInfo, after.MatchInfo),
            Events = MatchEvent.Merge(before.Events, after.Events),
            Users = MatchUser.Merge(before.Users, after.Users)
        };
    }
}