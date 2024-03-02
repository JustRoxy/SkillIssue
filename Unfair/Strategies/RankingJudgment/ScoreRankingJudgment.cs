using SkillIssue.Domain.Unfair.Entities;

namespace Unfair.Strategies.RankingJudgment;

public class ScoreRankingJudgment : IRankingJudgment
{
    public static readonly ScoreRankingJudgment Instance = new();

    public IReadOnlyList<Score> Rank(List<Score> bucket)
    {
        return bucket.OrderByDescending(x => x.TotalScore).ToList();
    }
}