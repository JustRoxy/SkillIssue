namespace SkillIssue.Domain.Unfair.Entities;

public class PlayerHistory
{
    public int PlayerId { get; set; }
    public int MatchId { get; set; }

    public double MatchCost { get; set; }
    public Player Player { get; set; } = null!;
    public TournamentMatch Match { get; set; } = null!;
}