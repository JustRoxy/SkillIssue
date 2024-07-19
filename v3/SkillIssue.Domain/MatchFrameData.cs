namespace SkillIssue.Domain;

public class MatchFrameData : IEquatable<MatchFrameData>
{
    public int MatchId { get; init; }
    public long Cursor { get; init; }
    public byte[] Frame { get; init; } = [];

    public bool Equals(MatchFrameData? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return MatchId == other.MatchId && Cursor == other.Cursor;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MatchFrameData)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MatchId, Cursor);
    }

    public static bool operator ==(MatchFrameData? left, MatchFrameData? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MatchFrameData? left, MatchFrameData? right)
    {
        return !Equals(left, right);
    }
}