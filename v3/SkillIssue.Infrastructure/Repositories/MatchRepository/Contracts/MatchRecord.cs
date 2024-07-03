namespace SkillIssue.Infrastructure.Repositories.MatchRepository.Contracts;

public struct MatchRecord
{
    public MatchRecord()
    {
    }

    public long MatchId { get; set; } = 0;
    public string Name { get; set; } = "";
    public int Status { get; set; } = 0;
    public bool IsTournament { get; set; } = false;
    public byte[]? Content { get; set; } = null;
    public DateTime StartTime { get; set; } = default;
    public DateTime? EndTime { get; set; } = null;
}