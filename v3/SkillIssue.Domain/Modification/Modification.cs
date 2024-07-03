namespace SkillIssue.Domain.Modification;

/// <summary>
///     Modification describes the mod aspect of the player, where <see cref="Attribute.Generic"/> is a common attribute
/// </summary>
public class Modification(Modification.Attribute attribute) : IEquatable<Modification>
{
    public Attribute ModificationId { get; } = attribute;

    public static readonly Modification Default = new(Attribute.Generic);

    public enum Attribute : short
    {
        Generic,
        Nomod,
        Hidden,
        HardRock,
        DoubleTime,
        Easy,
        Flashlight
    }

    #region Equality

    public bool Equals(Modification? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ModificationId == other.ModificationId;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Modification)obj);
    }

    public override int GetHashCode()
    {
        return (int)ModificationId;
    }

    public static bool operator ==(Modification? left, Modification? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Modification? left, Modification? right)
    {
        return !Equals(left, right);
    }

    #endregion
}