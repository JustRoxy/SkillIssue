using SkillIssue.Domain.Unfair.Entities;

namespace Unfair.Strategies.Ratings;

public class CachedRatingRepository(
    Dictionary<(int, int), Rating>? precache = null,
    bool hideSource = false,
    Action<Rating>? onCreation = null)
    : IRatingRepository
{
    public readonly Dictionary<(int playerId, int ratingAttributeId), Rating> Cache = precache ?? [];

    public bool GetRating(int playerId, int ratingAttributeId, out Rating rating)
    {
        if (Cache.TryGetValue((playerId, ratingAttributeId), out rating!)) return false;
        var newRating = new Rating
        {
            RatingAttributeId = ratingAttributeId,
            PlayerId = playerId,
            Mu = 25d,
            Sigma = 25d / 3,
            StarRatings = [],
            Ordinal = 0,
            GamesPlayed = 0
        };

        Cache[(playerId, ratingAttributeId)] = newRating;
        rating = newRating;

        onCreation?.Invoke(rating);

        return !hideSource;
    }

    public void SetRating(Rating rating)
    {
        Cache[(rating.PlayerId, rating.RatingAttributeId)] = rating;
    }
}