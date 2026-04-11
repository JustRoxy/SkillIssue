// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using SkillIssue.Domain.Unfair.Entities;

namespace SkillIssue.Domain.Events.Matches;

public class MatchCalculated : BaseEvent
{
    public required TournamentMatch Match { get; set; }
    public required List<RatingHistory> RatingChanges { get; set; }
    public required List<PlayerHistory> PlayerHistories { get; set; }
}