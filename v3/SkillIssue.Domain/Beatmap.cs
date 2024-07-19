namespace SkillIssue.Domain;

public class Beatmap : IEquatable<Beatmap>
{
    public int BeatmapId { get; init; }
    public BeatmapStatus Status { get; set; } = BeatmapStatus.Unknown;
    public byte[]? Content { get; set; }
    public DateTimeOffset LastUpdate { get; set; }

    public enum BeatmapStatus
    {
        Unknown = 0,
        NeedsUpdate = 1,
        NotFound = 2, //FAILED, END
        FailedToCalculateDifficulty = 3, //FAILED, END
        UpdatedSuccessfully = 4 //SUCCEDDED, END
    }

    public bool Equals(Beatmap? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return BeatmapId == other.BeatmapId;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Beatmap)obj);
    }

    public override int GetHashCode()
    {
        return BeatmapId;
    }

    public static bool operator ==(Beatmap? left, Beatmap? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Beatmap? left, Beatmap? right)
    {
        return !Equals(left, right);
    }
}