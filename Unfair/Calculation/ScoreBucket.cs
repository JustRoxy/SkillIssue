using SkillIssue.Domain.PPC.Entities;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;

namespace Unfair.Calculation;

public class Skillset
{
    public SkillsetRatingAttribute Attribute { get; set; }
    public BeatmapPerformance? BeatmapPerformance { get; set; }
}

public class ScoreBucket
{
    public ScoreBucket(IReadOnlyList<Score> scores,
        ModificationRatingAttribute modificationAttribute,
        Skillset skillset,
        ScoringRatingAttribute scoringAttribute)
    {
        Scores = scores;
        ModificationAttribute = modificationAttribute;
        Skillset = skillset;
        ScoringAttribute = scoringAttribute;
    }

    public IReadOnlyList<Score> Scores { get; }

    public ModificationRatingAttribute ModificationAttribute { get; }
    public Skillset Skillset { get; }
    public ScoringRatingAttribute ScoringAttribute { get; }
}