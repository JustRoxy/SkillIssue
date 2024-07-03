using SkillIssue.Domain;

namespace SkillIssue.Application.Services.IsTournamentMatch;

public interface IIsTournamentMatch
{
    public bool IsTournamentMatch(Match match);
}