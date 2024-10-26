using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MongoDB.Bson.Serialization.Attributes;
using SkillIssue.Matches.Contracts.Events;

namespace SkillIssue.Matches.Contracts;

public partial class MatchResponse
{
    [BsonId] [JsonPropertyName("match_id")]
    public int MatchId => MatchInfo.MatchId;

    [BsonElement]
    public bool IsNameInTournamentFormat => TournamentNameParser.IsMatch(MatchInfo.Name);

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

    private static readonly Regex TournamentNameParser = TournamentNameRegex();

    [GeneratedRegex(@"(?'acronym'.+):\s*(?'red'\(*.+\)*)\s*vs\s*(?'blue'\(*.+\)*)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "")]
    private static partial Regex TournamentNameRegex();
}