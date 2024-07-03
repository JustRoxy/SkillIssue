using SkillIssue.Common;

namespace SkillIssue.Domain.Scoring;

/// <summary>
///     Scoring describes the ranking process of the judgment
/// </summary>
/// <param name="attribute"></param>
public class Scoring(Scoring.Attribute attribute) : IScoring, IEquatable<Scoring>
{
    public Attribute ScoringId { get; } = attribute;
    public static readonly Scoring Default = new(Attribute.Score);

    public static readonly IReadOnlySet<Scoring> All =
        EnumExtensions.GetAllValues<Attribute, Scoring>(attribute => new Scoring(attribute));

    public enum Attribute
    {
        /// <summary>
        ///     Represents ScoreV1 and ScoreV2 scorings
        /// </summary>
        Score,

        /// <summary>
        ///     Represents Accuracy scoring
        /// </summary>
        Accuracy,

        /// <summary>
        ///     Represents MaxCombo scoring
        /// </summary>
        Combo,

        /// <summary>
        ///     Represents PP scoring
        /// </summary>
        Pp
    }


    public IEnumerable<Score> Score(IEnumerable<Score> scores)
    {
        return ScoringId switch
        {
            Attribute.Score => ScoreScoring(scores),
            Attribute.Accuracy => AccuracyScoring(scores),
            Attribute.Combo => ComboScoring(scores),
            Attribute.Pp => PpScoring(scores),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private IEnumerable<Score> ScoreScoring(IEnumerable<Score> scores) =>
        scores.OrderByDescending(score => score.TotalScore);

    private IEnumerable<Score> AccuracyScoring(IEnumerable<Score> scores) =>
        scores.OrderByDescending(score => score.Accuracy);

    private IEnumerable<Score> ComboScoring(IEnumerable<Score> scores) =>
        scores.OrderByDescending(score => score.MaxCombo);

    private IEnumerable<Score> PpScoring(IEnumerable<Score> scores) =>
        scores.OrderByDescending(score => score.Pp);


    #region Equality

    public bool Equals(Scoring? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ScoringId == other.ScoringId;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Scoring)obj);
    }

    public override int GetHashCode()
    {
        return (int)ScoringId;
    }

    public static bool operator ==(Scoring? left, Scoring? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Scoring? left, Scoring? right)
    {
        return !Equals(left, right);
    }

    #endregion
}