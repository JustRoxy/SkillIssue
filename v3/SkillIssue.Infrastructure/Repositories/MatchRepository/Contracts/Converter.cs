using SkillIssue.Domain;

namespace SkillIssue.Infrastructure.Repositories.MatchRepository.Contracts;

public static class Converter
{
    public static Match ToDomain(this MatchRecord record)
    {
        return new Match
        {
            MatchId = record.MatchId,
            Name = record.Name,
            MatchStatus = (Match.Status)record.Status,
            IsTournament = record.IsTournament,
            StartTime = record.StartTime,
            EndTime = record.EndTime,
            Cursor = record.Cursor,
        };
    }

    public static MatchRecord FromDomain(this Match match)
    {
        return new MatchRecord
        {
            MatchId = match.MatchId,
            Name = match.Name,
            Status = (int)match.MatchStatus,
            IsTournament = match.IsTournament,
            StartTime = match.StartTime,
            EndTime = match.EndTime,
            Cursor = match.Cursor,
        };
    }
}