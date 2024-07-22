using System.Text.Json.Serialization;
using SkillIssue.Common;
using SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts.Match.Events;

namespace SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts.Match;

//TODO: it's being used so much it should be an entity in Domain as well
public class MatchFrame : IMergable<MatchFrame>
{
    [JsonPropertyName("match")] public MatchInfo MatchInfo { get; init; } = new();
    [JsonPropertyName("first_event_id")] public long FirstEventId { get; init; }
    [JsonPropertyName("latest_event_id")] public long LatestEventId { get; init; }
    [JsonPropertyName("current_game_id")] public long? CurrentGameId { get; init; }
    [JsonPropertyName("events")] public IList<MatchEvent> Events { get; init; } = [];
    [JsonPropertyName("users")] public IList<MatchUser> Users { get; init; } = [];

    public static MatchFrame Merge(MatchFrame before, MatchFrame after)
    {
        return new MatchFrame()
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