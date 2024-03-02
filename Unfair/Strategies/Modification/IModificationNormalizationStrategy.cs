using osu.Game.Beatmaps.Legacy;

namespace Unfair.Strategies.Modification;

public interface IModificationNormalizationStrategy
{
    public LegacyMods Normalize(LegacyMods mod);
}