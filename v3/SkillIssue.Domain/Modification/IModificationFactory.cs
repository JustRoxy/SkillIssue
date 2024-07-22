namespace SkillIssue.Domain.Modification;

/// <summary>
///     Lazer changes the way mods work and because of it the implementation is in <see cref="SkillIssue.ThirdParty.OsuGame.ModificationFactory"/>
/// </summary>
public interface IModificationFactory
{
    public Modification? GetModification(IMods mods);
}