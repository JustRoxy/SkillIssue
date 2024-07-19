namespace SkillIssue.Infrastructure.Repositories.MatchRepository.Contracts;

public struct MatchRecord
{
    public MatchRecord()
    {
    }

    public int MatchId { get; set; } = 0;
    public string Name { get; set; } = "";
    public int Status { get; set; } = 0;
    public bool IsTournament { get; set; } = false;
    public DateTimeOffset StartTime { get; set; } = default;
    public DateTimeOffset? EndTime { get; set; } = null;
    public long? Cursor { get; set; } = null;
}