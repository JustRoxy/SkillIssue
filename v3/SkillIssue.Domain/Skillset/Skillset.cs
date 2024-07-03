using SkillIssue.Common;

namespace SkillIssue.Domain.Skillset;

public class Skillset(Skillset.Attribute attribute) : IEquatable<Skillset>
{
    public Attribute SkillsetId { get; } = attribute;
    public static readonly Skillset Default = new(Attribute.Overall);

    public static readonly IReadOnlySet<Skillset> All =
        EnumExtensions.GetAllValues<Attribute, Skillset>(attribute => new Skillset(attribute));

    public enum Attribute : short
    {
        Overall,
        Aim,
        Tapping,
        Technical,
        LowAr,
        HighAr,
        HighBpm,
        Precision
    }

    /// <summary>
    ///     Precision bounds to Hard Rock <br/>
    ///     High AR bounds to Double Time
    /// </summary>
    public bool ValidateModificationBounding(Modification.Modification modification)
    {
        if (SkillsetId == Attribute.Precision)
            return modification.ModificationId == Modification.Modification.Attribute.HardRock;
        if (SkillsetId == Attribute.HighAr)
            return modification.ModificationId == Modification.Modification.Attribute.DoubleTime;

        return true;
    }

    #region Equality

    public bool Equals(Skillset? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return SkillsetId == other.SkillsetId;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Skillset)obj);
    }

    public override int GetHashCode()
    {
        return (int)SkillsetId;
    }

    public static bool operator ==(Skillset? left, Skillset? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Skillset? left, Skillset? right)
    {
        return !Equals(left, right);
    }

    #endregion
}