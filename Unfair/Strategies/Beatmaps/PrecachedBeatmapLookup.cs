using osu.Game.Beatmaps.Legacy;
using SkillIssue.Domain.Extensions;
using SkillIssue.Domain.PPC.Entities;

namespace Unfair.Strategies.Beatmaps;

public class PrecachedBeatmapLookup(IReadOnlyDictionary<(int, int), BeatmapPerformance> performances) : IBeatmapLookup
{
    public BeatmapPerformance? LookupPerformance(int? beatmapId, LegacyMods mods)
    {
        if (beatmapId is null) return null;
        return performances!.GetValueOrDefault(
            (beatmapId.Value, (int)mods.NormalizeToPerformance()), null);
    }
}