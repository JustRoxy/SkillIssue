using SkillIssue.Domain.Unfair.Entities;

namespace Unfair.Strategies.RankingJudgment;

public class PpRankingStrategy : IRankingJudgment
{
    public static readonly PpRankingStrategy Instance = new();

    public IReadOnlyList<Score> Rank(List<Score> bucket)
    {
        return bucket.AsReadOnly();
    }
}