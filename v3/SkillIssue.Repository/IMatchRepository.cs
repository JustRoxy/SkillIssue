using SkillIssue.Domain;

namespace SkillIssue.Repository;

public interface IMatchRepository
{
    public Task<long> FindLastMatchId(CancellationToken cancellationToken);
    Task UpsertMatchesWithBulk(IEnumerable<Match> matches, CancellationToken cancellationToken);
    Task<IEnumerable<Match>> FindMatchesInStatus(Match.Status status, int limit, bool preoritizeTournamentMatches,
        CancellationToken cancellationToken);
}