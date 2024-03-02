using SkillIssue.Domain.Unfair.Entities;

namespace SkillIssue.Domain.Services;

public interface IBannedTournament
{
    public bool IsBanned(TournamentMatch match);
}