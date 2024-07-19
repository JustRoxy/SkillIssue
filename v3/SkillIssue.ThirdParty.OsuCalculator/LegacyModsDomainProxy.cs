using osu.Game.Beatmaps.Legacy;
using SkillIssue.Domain;

namespace SkillIssue.ThirdParty.OsuCalculator;

public class LegacyModsDomainProxy : IMods
{
    public LegacyMods LegacyMods { get; }

    public LegacyModsDomainProxy(LegacyMods legacyMods)
    {
        LegacyMods = legacyMods;
    }

    public object ToDatabase()
    {
        return (int)LegacyMods;
    }

    public static IMods FromLegacy(int legacyMods)
    {
        return new LegacyModsDomainProxy((LegacyMods)legacyMods);
    }
}