// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

namespace SkillIssue.Domain.Events.Beatmaps;

public class BeatmapProcessed : BaseEvent
{
    public required int BeatmapId { get; set; }
}