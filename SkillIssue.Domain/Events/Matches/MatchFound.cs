using SkillIssue.Domain.TGML.Entities;

namespace SkillIssue.Domain.Events.Matches;

public class MatchFound : BaseEvent
{
    public required TgmlMatch Match { get; set; }
}