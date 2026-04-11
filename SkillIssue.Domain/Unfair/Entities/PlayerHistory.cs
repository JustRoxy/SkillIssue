// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

namespace SkillIssue.Domain.Unfair.Entities;

public class PlayerHistory
{
    public int PlayerId { get; set; }
    public int MatchId { get; set; }

    public double MatchCost { get; set; }
    public Player Player { get; set; } = null!;
    public TournamentMatch Match { get; set; } = null!;
}