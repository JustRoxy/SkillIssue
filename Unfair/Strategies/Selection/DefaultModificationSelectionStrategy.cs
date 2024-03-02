using osu.Game.Beatmaps.Legacy;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;
using Unfair.Strategies.Modification;

namespace Unfair.Strategies.Selection;

public class DefaultModificationSelectionStrategy
{
    public static DefaultModificationSelectionStrategy Instance => new();

    public List<ModificationRatingAttribute> Select(LegacyMods mods)
    {
        List<ModificationRatingAttribute> modifications = [ModificationRatingAttribute.AllMods];
        var normalizedMod = TrimmingModificationStrategy.Instance.Normalize(mods);

        switch (normalizedMod)
        {
            case LegacyMods.None:
                modifications.Add(ModificationRatingAttribute.NM);
                break;
            case LegacyMods.DoubleTime or (LegacyMods.DoubleTime | LegacyMods.Hidden):
                modifications.Add(ModificationRatingAttribute.DT);
                break;
            case LegacyMods.HardRock or (LegacyMods.HardRock | LegacyMods.Hidden):
                modifications.Add(ModificationRatingAttribute.HR);
                break;
            case LegacyMods.Hidden:
                modifications.Add(ModificationRatingAttribute.HD);
                break;
        }

        return modifications;
    }

    public List<ModificationRatingAttribute> Select(Score score)
    {
        return Select(score.LegacyMods);
    }
}