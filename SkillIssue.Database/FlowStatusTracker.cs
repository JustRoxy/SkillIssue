using SkillIssue.Domain;

namespace SkillIssue.Database;

public class FlowStatusTracker : BaseEntity
{
    public int MatchId { get; set; }
    public FlowStatus Status { get; set; }
}

public enum FlowStatus
{
    Created,
    TgmlFetched,
    Done
}