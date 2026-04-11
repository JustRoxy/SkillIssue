// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using osu.Game.Beatmaps.Legacy;
using SkillIssue.Domain.PPC.Entities;

namespace Unfair.Strategies.Beatmaps;

public interface IBeatmapLookup
{
    public BeatmapPerformance? LookupPerformance(int? beatmapId, LegacyMods mods);
}