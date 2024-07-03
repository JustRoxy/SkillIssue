using System.Text.RegularExpressions;
using Match = SkillIssue.Domain.Match;

namespace SkillIssue.Application.Services.IsTournamentMatch;

public class IsTournamentMatchValidator : IIsTournamentMatch
{
    private static readonly Regex TournamentMatchRegex =
        new(@".*:\s?\(?(.*)\)?\s.*vs.*\s\(?(.*)\)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool IsTournamentMatch(Match match)
    {
        return TournamentMatchRegex.IsMatch(match.Name);
    }
}