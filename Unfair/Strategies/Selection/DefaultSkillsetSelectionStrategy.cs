using SkillIssue.Domain.PPC.Entities;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;
using Unfair.Calculation;

namespace Unfair.Strategies.Selection;

public class DefaultSkillsetSelectionStrategy
{
    public static readonly DefaultSkillsetSelectionStrategy Instance = new();

    public List<Skillset> Select(ModificationRatingAttribute modification, BeatmapPerformance? beatmapPerformance)
    {
        if (beatmapPerformance is null) return [new Skillset { Attribute = SkillsetRatingAttribute.Overall }];

        List<SkillsetRatingAttribute> skillsets = [SkillsetRatingAttribute.Overall];
        if (beatmapPerformance.AimDifficulty > beatmapPerformance.SpeedDifficulty + 0.2)
            skillsets.Add(SkillsetRatingAttribute.Aim);

        if (beatmapPerformance.SpeedDifficulty > beatmapPerformance.AimDifficulty + 0.2)
            skillsets.Add(SkillsetRatingAttribute.Tapping);

        if (beatmapPerformance.Bpm >= 225) skillsets.Add(SkillsetRatingAttribute.HighBpm);

        if (beatmapPerformance.SliderFactor <= 0.97d) skillsets.Add(SkillsetRatingAttribute.Technical);

        if (RatingAttribute.UsableRatingAttribute(modification, SkillsetRatingAttribute.Precision))
            if (beatmapPerformance.CircleSize >= 6.5)
                skillsets.Add(SkillsetRatingAttribute.Precision);

        if (RatingAttribute.UsableRatingAttribute(modification, SkillsetRatingAttribute.LowAR))
            if (beatmapPerformance.ApproachRate <= 8.5)
                skillsets.Add(SkillsetRatingAttribute.LowAR);

        if (RatingAttribute.UsableRatingAttribute(modification, SkillsetRatingAttribute.HighAR))
            if (modification == ModificationRatingAttribute.DT)
                if (beatmapPerformance.ApproachRate > 10.3)
                    skillsets.Add(SkillsetRatingAttribute.HighAR);

        return skillsets.Select(x => new Skillset
        {
            Attribute = x,
            BeatmapPerformance = beatmapPerformance
        }).ToList();
    }
}