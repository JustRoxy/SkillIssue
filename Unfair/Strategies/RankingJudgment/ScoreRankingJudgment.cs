// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

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