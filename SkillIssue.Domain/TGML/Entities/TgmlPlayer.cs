namespace SkillIssue.Domain.TGML.Entities;

public class TgmlPlayer
{
    public int PlayerId { get; set; }
    public string CurrentUsername { get; set; } = null!;

    public IList<TgmlMatch> Matches { get; set; }
}