namespace SkillIssue.Domain;

public class Score
{
    public long GameId { get; set; }
    public long PlayerId { get; set; }
    public int TotalScore { get; set; }
    public int MaxCombo { get; set; }
    public double Accuracy { get; set; }
    public bool IsFreemod { get; set; }
    public double Pp { get; set; } = 0d;
    public IMods Mods { get; set; } = default!;

    public Modification.Modification Modification { get; set; } = Domain.Modification.Modification.Default;
    public List<Skillset.Skillset> Skillset { get; set; } = [Domain.Skillset.Skillset.Default];
}