namespace SkillIssue.Domain.Discord;

public class InteractionState
{
    public ulong CreatorId { get; set; }
    public ulong MessageId { get; set; }

    public int? PlayerId { get; set; }
    public DateTime CreationTime { get; set; }

    public string StatePayload { get; set; } = null!;
}