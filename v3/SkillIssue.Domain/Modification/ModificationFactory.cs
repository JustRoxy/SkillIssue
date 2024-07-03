using osu.Game.Beatmaps.Legacy;

namespace SkillIssue.Domain.Modification;

public class ModificationFactory : IModificationFactory
{
    private const LegacyMods UselessMods = LegacyMods.NoFail | LegacyMods.SuddenDeath | LegacyMods.Nightcore |
                                           LegacyMods.SpunOut | LegacyMods.Perfect | LegacyMods.ScoreV2;

    public Modification? GetModification(LegacyMods mods)
    {
        var normalizedMods = mods & ~UselessMods;
        var gameModification = GetGameModification(normalizedMods);

        return gameModification;
    }

    private Modification? GetGameModification(LegacyMods normalizedMods)
    {
        if (normalizedMods == LegacyMods.None)
            return new Modification(Modification.Attribute.Nomod);

        if (normalizedMods == LegacyMods.Hidden)
            return new Modification(Modification.Attribute.Hidden);

        if (normalizedMods.HasFlag(LegacyMods.Flashlight))
            return new Modification(Modification.Attribute.Flashlight);

        if (normalizedMods.HasFlag(LegacyMods.Easy))
            return new Modification(Modification.Attribute.Easy);

        if (normalizedMods.HasFlag(LegacyMods.DoubleTime))
            return new Modification(Modification.Attribute.DoubleTime);

        if (normalizedMods.HasFlag(LegacyMods.HardRock))
            return new Modification(Modification.Attribute.HardRock);

        return null;
    }
}