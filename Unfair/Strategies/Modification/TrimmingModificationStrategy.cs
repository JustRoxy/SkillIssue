using osu.Game.Beatmaps.Legacy;

namespace Unfair.Strategies.Modification;

public class TrimmingModificationStrategy : IModificationNormalizationStrategy
{
    public const LegacyMods UselessMods = LegacyMods.NoFail | LegacyMods.SuddenDeath | LegacyMods.Nightcore |
                                          LegacyMods.SpunOut |
                                          LegacyMods.Perfect | LegacyMods.ScoreV2;

    public static readonly TrimmingModificationStrategy Instance = new();

    public LegacyMods Normalize(LegacyMods mod)
    {
        return mod & ~UselessMods;
    }
}