using SkillIssue.Domain;

namespace SkillIssue.Repository;

public interface IMatchRepository
{
    public Task<long?> FindLastMatchId(CancellationToken cancellationToken);
    Task UpsertMatchesWithBulk(IEnumerable<Match> matches, CancellationToken cancellationToken);

    Task<IEnumerable<Match>> FindMatchesInStatus(Match.Status status, int limit, bool preoritizeTournamentMatches,
        CancellationToken cancellationToken);

    public Task ChangeMatchStatusToCompleted(int matchId, DateTimeOffset endedAt, CancellationToken cancellationToken);
    public Task ChangeMatchStatus(int matchId, Match.Status status, CancellationToken cancellationToken);

    public Task UpdateMatchCursorWithLastTimestamp(int matchId, long cursor, DateTimeOffset lastTimestamp,
        CancellationToken cancellationToken);
}