namespace SkillIssue.Matches.Models;

public class MatchFrame
{
    public int MatchId { get; set; }
    public long Cursor { get; set; }
    public byte[] Data { get; set; } = [];
}