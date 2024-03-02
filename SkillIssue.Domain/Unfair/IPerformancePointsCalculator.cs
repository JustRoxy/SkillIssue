using SkillIssue.Domain.PPC.Entities;
using SkillIssue.Domain.Unfair.Entities;

namespace SkillIssue.Domain.Unfair;

public interface IPerformancePointsCalculator
{
    public Task<double?> CalculatePerformancePoints(BeatmapPerformance beatmapPerformance, Score score,
        CancellationToken token);
}