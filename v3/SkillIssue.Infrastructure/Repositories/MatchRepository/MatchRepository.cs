using Dapper;
using NuGet.Packaging.Licenses;
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

    public async Task<long> FindLastMatchId(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.GetConnectionAsync();

        var command = new CommandDefinition(MatchQueries.FindLastMatchId(), cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<long>(command);
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