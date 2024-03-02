using osu.Game.Beatmaps.Legacy;

namespace SkillIssue.Domain.Extensions;

public static class LegacyModsExtensions
{
    private const LegacyMods DifficultyChangingMods = LegacyMods.TouchDevice | LegacyMods.DoubleTime |
                                                      LegacyMods.HalfTime | LegacyMods.Easy | LegacyMods.HardRock |
                                                      LegacyMods.Flashlight;

    public static LegacyMods NormalizeToPerformance(this LegacyMods mods)
    {
        var normalized = mods & DifficultyChangingMods;
        if (normalized.HasFlag(LegacyMods.Hidden) && !normalized.HasFlag(LegacyMods.Flashlight))
            normalized &= ~LegacyMods.Hidden;

        return normalized;
    }
}