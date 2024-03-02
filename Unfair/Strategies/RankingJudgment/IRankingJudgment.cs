using SkillIssue.Domain.Unfair.Entities;

namespace Unfair.Strategies.RankingJudgment;

public interface IRankingJudgment
{
    public IReadOnlyList<Score> Rank(List<Score> bucket);
}