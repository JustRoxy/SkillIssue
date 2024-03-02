using osu.Game.Beatmaps.Legacy;
using SkillIssue.Domain.Extensions;
using SkillIssue.Domain.PPC.Entities;

namespace Unfair.Strategies.Beatmaps;

public class CacheBeatmapLookupStrategy(IReadOnlyDictionary<int, BeatmapPerformance> lookupCache) : IBeatmapLookup
{
    public BeatmapPerformance? LookupPerformance(int? beatmapId, LegacyMods mods)
    {
        if (beatmapId is null) return null;

        var normalized = mods.NormalizeToPerformance();
        return lookupCache!.GetValueOrDefault((int)normalized, null);
    }
}