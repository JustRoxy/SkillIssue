using osu.Game.Beatmaps.Legacy;
using SkillIssue.Domain.PPC.Entities;

namespace Unfair.Strategies.Beatmaps;

public interface IBeatmapLookup
{
    public BeatmapPerformance? LookupPerformance(int? beatmapId, LegacyMods mods);
}