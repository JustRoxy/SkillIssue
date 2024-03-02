using SkillIssue.Domain.Unfair.Enums;

namespace Unfair.Strategies.RankingJudgment;

public class RankingJudgmentFactory
{
    public static readonly RankingJudgmentFactory Instance = new();

    public IRankingJudgment CreateJudgment(ScoringRatingAttribute scoringRatingAttribute)
    {
        return scoringRatingAttribute switch
        {
            ScoringRatingAttribute.Score => ScoreRankingJudgment.Instance,
            ScoringRatingAttribute.Combo => ComboRankingJudgment.Instance,
            ScoringRatingAttribute.Accuracy => AccuracyRankingJudgment.Instance,
            ScoringRatingAttribute.PP => PpRankingStrategy.Instance,
            _ => throw new ArgumentOutOfRangeException(nameof(scoringRatingAttribute), scoringRatingAttribute, null)
        };
    }
}