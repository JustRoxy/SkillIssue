using SkillIssue.Domain;
using SkillIssue.ThirdParty.API.Osu.Queries.GetMatchPage.Contracts;

namespace SkillIssue.Application.Commands.Stage1FindNewMatches.Contracts;

public static class Converters
{
    public static Match ToDomain(this GetMatchPageItem item)
    {
        return new Match
        {
            MatchId = item.Id,
            Name = item.Name,
            StartTime = item.StartTime,
            EndTime = item.EndTime
        };
    }
}