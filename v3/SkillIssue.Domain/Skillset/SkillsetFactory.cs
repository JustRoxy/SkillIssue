namespace SkillIssue.Domain.Skillset;

public class SkillsetFactory : ISkillsetFactory
{
    public IReadOnlyList<Skillset> GetBeatmapSkillsets(Modification.Modification modification, BeatmapDifficulty? difficulty)
    {
        List<Skillset> skillsets = [Skillset.Default];
        if (difficulty is null) return skillsets;

        if (difficulty.AimDifficulty > difficulty.SpeedDifficulty + 0.2)
            skillsets.Add(new Skillset(Skillset.Attribute.Aim));

        if (difficulty.SpeedDifficulty > difficulty.AimDifficulty + 0.2)
            skillsets.Add(new Skillset(Skillset.Attribute.Tapping));

        if (difficulty.Bpm >= 225) skillsets.Add(new Skillset(Skillset.Attribute.HighBpm));

        if (difficulty.SliderFactor <= 0.97d) skillsets.Add(new Skillset(Skillset.Attribute.Technical));

        if (difficulty.CircleSize >= 6.5)
            skillsets.Add(new Skillset(Skillset.Attribute.Precision));

        if (difficulty.ApproachRate <= 8.5)
            skillsets.Add(new Skillset(Skillset.Attribute.LowAr));

        if (difficulty.ApproachRate > 10.3)
            skillsets.Add(new Skillset(Skillset.Attribute.HighAr));

        return skillsets.Where(skillset => skillset.ValidateModificationBounding(modification)).ToList();
    }
}