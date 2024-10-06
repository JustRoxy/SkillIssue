namespace SkillIssue.Common.Contracts.Matches;

public class NewMatchesEvent
{
    public class NewMatch
    {
        public required int MatchId { get; set; }
        public required DateTimeOffset StartTime { get; set; }
        public required string Name { get; set; }
    }

    public required List<NewMatch> NewMatches { get; set; }
}