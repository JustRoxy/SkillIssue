namespace SkillIssue.Domain.Modification;

/// <summary>
///     Lazer changes the way mods work and because of it the implementation is in <see cref="SkillIssue.ThirdParty.OsuCalculator.ModificationFactory"/>
/// </summary>
public interface IModificationFactory
{
    public Modification? GetModification(IMods mods);
}