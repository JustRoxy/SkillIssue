using osu.Game.Beatmaps.Legacy;
using SkillIssue.Domain;

namespace SkillIssue.ThirdParty.OsuGame;

public class LegacyModsDomainProxy(LegacyMods legacyMods) : IMods
{
    public LegacyMods LegacyMods { get; } = legacyMods;

    public object ToDatabase()
    {
        return (int)LegacyMods;
    }

    public static IMods FromLegacy(int legacyMods)
    {
        return new LegacyModsDomainProxy((LegacyMods)legacyMods);
    }
}