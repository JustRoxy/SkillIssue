using SkillIssue.Domain.Services;
using SkillIssue.Domain.Unfair.Entities;

namespace Unfair.Services;

public class UnfairBannedTournament : IBannedTournament
{
    private static readonly HashSet<string> BannedAcronyms = ["ETX", "o!mm", "PSK", "TGC", "NDC2", "FEM2", "ROMAI", "MEM"];

    public bool IsBanned(TournamentMatch match)
    {
        ArgumentNullException.ThrowIfNull(match.Acronym, nameof(match.Acronym));

        return BannedAcronyms.Any(x => match.Acronym.StartsWith(x));
    }
}