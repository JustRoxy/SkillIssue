using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;
using Unfair.Calculation;
using Unfair.Strategies.Beatmaps;
using Unfair.Strategies.Modification;
using Unfair.Strategies.RankingJudgment;
using Unfair.Strategies.Selection;

namespace Unfair.Strategies.Freemods;

public class GroupingFreemodStrategy
{
    public readonly IReadOnlyCollection<ScoreBucket> Groups;

    public GroupingFreemodStrategy(IEnumerable<Score> scores,
        IModificationNormalizationStrategy modificationNormalizationStrategy,
        IBeatmapLookup beatmapLookup)
    {
        Groups = scores.GroupBy(x => modificationNormalizationStrategy.Normalize(x.LegacyMods))
            .SelectMany(x =>
            {
                var localScores = x.ToList();
                List<ScoreBucket> buckets = [];
                buckets.AddRange(
                    from scoring in Enum.GetValues<ScoringRatingAttribute>()
                    let rankedScores = RankingJudgmentFactory.Instance.CreateJudgment(scoring).Rank(localScores)
                    from modification in DefaultModificationSelectionStrategy.Instance.Select(x.First())
                    from skillset in DefaultSkillsetSelectionStrategy.Instance.Select(
                        modification, beatmapLookup.LookupPerformance(x.First().BeatmapId, x.Key), x.Key)
                    select new ScoreBucket(rankedScores, modification, skillset, scoring));

                return buckets;
            })
            .ToList();
    }
}