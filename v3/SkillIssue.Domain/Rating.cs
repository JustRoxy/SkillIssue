namespace SkillIssue.Domain;

public class Rating
{
    public Modification.Modification Modification { get; set; } = Domain.Modification.Modification.Default;
    public Skillset.Skillset Skillset { get; set; } = Domain.Skillset.Skillset.Default;
    public Scoring.Scoring Scoring { get; set; } = Domain.Scoring.Scoring.Default;

    public long PlayerId { get; set; }
    public double Mu { get; set; }
}