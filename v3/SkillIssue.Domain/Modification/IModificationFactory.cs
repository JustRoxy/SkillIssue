using osu.Game.Beatmaps.Legacy;

namespace SkillIssue.Domain.Modification;

public interface IModificationFactory
{
    public Modification? GetModification(LegacyMods mods);
}