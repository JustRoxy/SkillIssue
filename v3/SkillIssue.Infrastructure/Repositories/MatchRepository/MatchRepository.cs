using Dapper;
using SkillIssue.Domain;
using SkillIssue.Infrastructure.Repositories.MatchRepository.Contracts;
using SkillIssue.Repository;

namespace SkillIssue.Infrastructure.Repositories.MatchRepository;

public class MatchRepository : IMatchRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public MatchRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long?> FindLastMatchId(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.GetConnectionAsync();

        var command = new CommandDefinition(MatchQueries.FindLastMatchId(), cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<long?>(command);
    }

    public async Task ChangeMatchStatusToCompleted(int matchId, DateTimeOffset endedAt,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.GetConnectionAsync();

        var command = MatchQueries.MoveMatchToStatusWithEndTimeChange(matchId, (int)Match.Status.Completed, endedAt,
            cancellationToken);
        try
        {
            await connection.ExecuteAsync(command);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to change match status to completed. matchId: {matchId}, endedAt: {endedAt}",
                e);
        }
    }

    public async Task ChangeMatchStatus(int matchId, Match.Status status, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.GetConnectionAsync();

        var command = MatchQueries.MoveMatchToStatus(matchId, (int)status, cancellationToken);
        try
        {
            await connection.ExecuteAsync(command);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to change match status. matchId: {matchId}, status: {status}",
                e);
        }
    }

    public async Task UpdateMatchCursorWithLastTimestamp(int matchId, long cursor, DateTimeOffset lastTimestamp,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.GetConnectionAsync();

        var command = MatchQueries.UpdateMatchCursor(matchId, cursor, lastTimestamp, cancellationToken);
        try
        {
            await connection.ExecuteAsync(command);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to change match cursor. matchId: {matchId}, cursor: {cursor}",
                e);
        }
    }

    public async Task UpsertMatchesWithBulk(IEnumerable<Match> matches, CancellationToken cancellationToken)
    {
        var records = matches.Select(match => match.FromDomain());

        await using var connection = await _connectionFactory.GetConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var command = new CommandDefinition(MatchQueries.UpsertMatchesWithBulk(), records, transaction,
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IEnumerable<Match>> FindMatchesInStatus(Match.Status status, int limit,
        bool preoritizeTournamentMatches,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.GetConnectionAsync();

        var command =
            MatchQueries.FindMatchesInStatus((int)status, limit, preoritizeTournamentMatches, cancellationToken);
        var records = await connection.QueryAsync<MatchRecord>(command);
        return records.Select(record => record.ToDomain());
    }
}