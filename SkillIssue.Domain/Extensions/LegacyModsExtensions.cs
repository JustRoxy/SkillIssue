// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using osu.Game.Beatmaps.Legacy;

namespace SkillIssue.Domain.Extensions;

public static class LegacyModsExtensions
{
    private const LegacyMods DifficultyChangingMods = LegacyMods.TouchDevice |
                                                      LegacyMods.DoubleTime |
                                                      LegacyMods.HalfTime |
                                                      LegacyMods.Easy |
                                                      LegacyMods.HardRock |
                                                      LegacyMods.Flashlight;

    public static LegacyMods NormalizeToPerformance(this LegacyMods mods)
    {
        var normalized = mods & DifficultyChangingMods;
        //TODO: fix this bug
        if (normalized.HasFlag(LegacyMods.Hidden) && !normalized.HasFlag(LegacyMods.Flashlight))
            normalized &= ~LegacyMods.Hidden;

        return normalized;
    }
}