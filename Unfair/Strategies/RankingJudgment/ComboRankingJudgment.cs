using SkillIssue.Domain.Unfair.Entities;

namespace Unfair.Strategies.RankingJudgment;

public class ComboRankingJudgment : IRankingJudgment
{
    public static readonly ComboRankingJudgment Instance = new();

    public IReadOnlyList<Score> Rank(List<Score> bucket)
    {
        return bucket.OrderByDescending(x => x.MaxCombo).ToList();
    }
}