using SkillIssue.Domain.Unfair.Entities;

namespace SkillIssue.Domain.Events.Matches;

public class MatchCalculated : BaseEvent
{
    public required TournamentMatch Match { get; set; }
    public required List<RatingHistory> RatingChanges { get; set; }
    public required List<PlayerHistory> PlayerHistories { get; set; }
}