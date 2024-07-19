using Dapper;
using Microsoft.Extensions.Logging;
using SkillIssue.Common;
using SkillIssue.Common.Utils;
using SkillIssue.Domain;
using SkillIssue.Infrastructure.Repositories.BeatmapRepository.Contracts;
using SkillIssue.Repository;

namespace SkillIssue.Infrastructure.Repositories.BeatmapRepository;

public class BeatmapRepository(IConnectionFactory connectionFactory, ILogger<BeatmapRepository> logger)
    : IBeatmapRepository
{
    public async Task InsertBeatmapsIfNotExistWithBulk(IEnumerable<Beatmap> beatmaps,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.GetConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var records = beatmaps.Select(beatmap => beatmap.FromDomain());
        var query = BeatmapQueries.InsertBeatmapsIfNotExistWithBulk(records, transaction, cancellationToken);

        try
        {
            await TimeMeasuring.MeasureAsync(logger, nameof(InsertBeatmapsIfNotExistWithBulk), async () =>
            {
                await connection.ExecuteAsync(query);
                await transaction.CommitAsync(cancellationToken);
            });
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new Exception("Failed to insert beatmaps with bulk", e);
        }
    }

    public async Task JournalizeMatchBeatmaps(IEnumerable<(int beatmapId, int matchId)> matchBeatmaps,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.GetConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var records = matchBeatmaps.Select(beatmap => beatmap.FromDomain());
        var query = BeatmapQueries.InsertMatchBeatmapRelationWithBulk(records, transaction, cancellationToken);

        try
        {
            await TimeMeasuring.MeasureAsync(logger, nameof(JournalizeMatchBeatmaps), async () =>
            {
                await connection.ExecuteAsync(query);
                await transaction.CommitAsync(cancellationToken);
            });
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new Exception("Failed to insert match beatmap relation with bulk", e);
        }
    }

    public async Task<IEnumerable<int>> GetMatchBeatmapsWithStatus(int matchId, Beatmap.BeatmapStatus status,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.GetConnectionAsync();

        var query = BeatmapQueries.FindMatchBeatmapsWithStatus(matchId, status, cancellationToken);

        try
        {
            return await TimeMeasuring.MeasureAsync(logger, nameof(GetMatchBeatmapsWithStatus),
                () => connection.QueryAsync<int>(query));
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to query match beatmaps with status. matchId: {matchId}, status: {status}", e);
        }
    }

    public async Task InsertDifficultiesWithBulk(IEnumerable<BeatmapDifficulty> difficulties,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.GetConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var query = BeatmapQueries.InsertDifficultiesWithBulk(difficulties, transaction, cancellationToken);

        try
        {
            await TimeMeasuring.MeasureAsync(logger, nameof(InsertDifficultiesWithBulk), async () =>
            {
                await connection.ExecuteAsync(query);
                await transaction.CommitAsync(cancellationToken);
            });
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new Exception("Failed to insert beatmap difficulties with bulk", e);
        }
    }

    public async Task UpdateBeatmap(Beatmap beatmap, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.GetConnectionAsync();

        var query = BeatmapQueries.UpdateBeatmap(beatmap.FromDomain(), cancellationToken);

        try
        {
            await TimeMeasuring.MeasureAsync(logger, nameof(UpdateBeatmap),
                async () => { await connection.ExecuteAsync(query); });
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Failed to update beatmap. beatmapId: {beatmap.BeatmapId}, status: {beatmap.Status}, content: {beatmap.Content?.GetPhysicalSizeInMegabytes() ?? 0:N2}mb",
                e);
        }
    }
}