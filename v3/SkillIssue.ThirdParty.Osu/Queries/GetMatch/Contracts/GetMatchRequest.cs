namespace SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts;

public class GetMatchRequest
{
    public int MatchId { get; set; }
    public long? Cursor { get; set; }
}