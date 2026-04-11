// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using osu.Game.Beatmaps.Legacy;

namespace Unfair.Strategies.Modification;

public class TrimmingModificationStrategy : IModificationNormalizationStrategy
{
    public const LegacyMods UselessMods = LegacyMods.NoFail | 
                                          LegacyMods.SuddenDeath | 
                                          LegacyMods.Nightcore |
                                          LegacyMods.SpunOut |
                                          LegacyMods.Perfect | 
                                          LegacyMods.ScoreV2;

    public static readonly TrimmingModificationStrategy Instance = new();

    public LegacyMods Normalize(LegacyMods mod)
    {
        return mod & ~UselessMods;
    }
}