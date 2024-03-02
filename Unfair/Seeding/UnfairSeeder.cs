using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;

namespace Unfair.Seeding;

public class UnfairSeeder(DatabaseContext context)
{
    private async Task SeedRatingAttributes()
    {
        var existingAttributes = await context.RatingAttributes.AsNoTracking().ToListAsync();
        var mods = Enum.GetValues<ModificationRatingAttribute>();
        var skillsets = Enum.GetValues<SkillsetRatingAttribute>();
        var scorings = Enum.GetValues<ScoringRatingAttribute>();
        foreach (var mod in mods)
        foreach (var skillset in skillsets)
        foreach (var scoring in scorings)
        {
            if (existingAttributes.Any(x =>
                    x.Modification == mod &&
                    x.Skillset == skillset &&
                    x.Scoring == scoring)) continue;

            context.RatingAttributes.Add(new RatingAttribute
            {
                AttributeId = RatingAttribute.GetAttributeId(mod, skillset, scoring),
                Modification = mod,
                Skillset = skillset,
                Scoring = scoring
            });
        }

        await context.SaveChangesAsync();
    }

    public async Task Seed()
    {
        await SeedRatingAttributes();
    }
}