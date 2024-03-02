using SkillIssue.Domain.Unfair.Entities;

namespace Unfair.Strategies.RankingJudgment;

public class AccuracyRankingJudgment : IRankingJudgment
{
    public static readonly AccuracyRankingJudgment Instance = new();

    public IReadOnlyList<Score> Rank(List<Score> bucket)
    {
        return bucket.OrderByDescending(x => x.Accuracy).ToList();
    }
}