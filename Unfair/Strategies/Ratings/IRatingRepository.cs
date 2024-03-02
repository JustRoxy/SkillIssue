using SkillIssue.Domain.Unfair.Entities;

namespace Unfair.Strategies.Ratings;

public interface IRatingRepository
{
    public bool GetRating(int playerId, int ratingAttributeId, out Rating rating);
    public void SetRating(Rating rating);
}