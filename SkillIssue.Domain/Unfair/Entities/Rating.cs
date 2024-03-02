using System.ComponentModel.DataAnnotations.Schema;
using MathNet.Numerics.Statistics;

namespace SkillIssue.Domain.Unfair.Entities;

public enum RatingStatus
{
    Calibration,
    Ranked,
    Inactive
}

public static class RatingExtensions
{
    public static IQueryable<Rating> Ranked(this IQueryable<Rating> query)
    {
        return query.Where(x => x.Status == RatingStatus.Ranked && !x.Player.IsRestricted);
    }

    public static IQueryable<Rating> Major(this IQueryable<Rating> query)
    {
        return query.Where(x => RatingAttribute.MajorAttributes.Contains(x.RatingAttributeId));
    }
}

public class Rating
{
    private const int OverallGameRequirement = 100;
    private const int AdditionalGameRequirement = 10;
    public int RatingAttributeId { get; init; }
    public int PlayerId { get; init; }

    public double Mu { get; set; }
    public double Sigma { get; set; }

    public List<double> StarRatings { get; set; } = [];

    public List<double> PerformancePoints { get; init; } = [];

    public RatingStatus Status
    {
        get => GetCurrentStatus();

        // ReSharper disable once ValueParameterNotUsed
        private set { }
    }

    public double Ordinal { get; set; }
    public double StarRating { get; set; }
    [NotMapped] public short OrdinalShort => (short)Math.Round(Ordinal, MidpointRounding.AwayFromZero);
    public int GamesPlayed { get; set; }
    public int WinAmount { get; set; }
    public int TotalOpponentsAmount { get; set; }

    public double Winrate
    {
        get => TotalOpponentsAmount == 0 ? 0 : (double)WinAmount / TotalOpponentsAmount;
        // ReSharper disable once ValueParameterNotUsed
        set { }
    }

    public Player Player { get; set; } = null!;
    public RatingAttribute RatingAttribute { get; init; } = null!;

    private double GetStarRating()
    {
        return StarRatings.Count == 0 ? 0 : StarRatings.UpperQuartile();
    }

    private static int GetRankedGameRequirement(int ratingAttributeId)
    {
        if (ratingAttributeId is 0 or 1 or 2 or 3) return OverallGameRequirement;
        return AdditionalGameRequirement;
    }

    public RatingStatus GetCurrentStatus()
    {
        return GamesPlayed < GetRankedGameRequirement(RatingAttributeId)
            ? RatingStatus.Calibration
            : RatingStatus.Ranked;
    }

    public void AddStarRating(double starRating)
    {
        StarRatings.Add(starRating);
        StarRating = GetStarRating();
    }

    public static List<Rating> Default(int playerId)
    {
        return RatingAttribute.GetAllAttributes()
            .Select(x => new Rating
            {
                RatingAttributeId = x.AttributeId,
                PlayerId = playerId,
                Mu = 25,
                Sigma = 25d / 3,
                StarRatings = [],
                Ordinal = 0,
                GamesPlayed = 0,
                RatingAttribute = x
            })
            .ToList();
    }
}