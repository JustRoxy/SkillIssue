// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using SkillIssue.Domain.PPC.Entities;
using SkillIssue.Domain.Unfair.Entities;

namespace SkillIssue.Domain.Unfair;

public interface IPerformancePointsCalculator
{
    public Task<double?> CalculatePerformancePoints(BeatmapPerformance beatmapPerformance, Score score,
        CancellationToken token);
}