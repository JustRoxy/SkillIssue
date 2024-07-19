using SkillIssue.Common.Exceptions;
using SkillIssue.Domain;

namespace SkillIssue.ThirdParty.OsuCalculator;

public class ModsFactory
{
    public static LegacyModsDomainProxy FromGeneric(IMods mods)
    {
        if (mods is LegacyModsDomainProxy lmdp) return lmdp;

        throw new SeriousValidationException($"expected {nameof(IMods)} to be {nameof(LegacyModsDomainProxy)}");
    }
}