using Dapper;
using SkillIssue.Common;
using SkillIssue.Domain;
using SkillIssue.Infrastructure.Repositories.MatchFrameRepository.Contracts;
using SkillIssue.Repository;

namespace SkillIssue.Infrastructure.Repositories.MatchFrameRepository;

public class MatchFrameRepository : IMatchFrameRepository
{
    private readonly IConnectionFactory _connectionFactory;

    public MatchFrameRepository(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<MatchFrameData>> GetMatchFramesWithBulk(IList<int> matchIds,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.GetConnectionAsync();

        var query = MatchFrameQueries.GetMatchFramesWithBulk(matchIds, cancellationToken);

        try
        {
            var result = await connection.QueryAsync<MatchFrameData>(query);
            return result;
        }
        catch (Exception e)
        {
            throw new Exception("Failed to query match frames", e);
        }
    }

    public async Task CacheFrame(MatchFrameData matchFrameData, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.GetConnectionAsync();

        var frameRecord = new MatchFrameRecord
        {
            MatchId = matchFrameData.MatchId,
            Cursor = matchFrameData.Cursor,
            Frame = matchFrameData.Frame,
        };

        var query = MatchFrameQueries.UpsertMatchFrame(frameRecord, cancellationToken);

        try
        {
            await connection.ExecuteAsync(query);
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Failed to save frame. matchId: {matchFrameData.MatchId}, eventId: {matchFrameData.Cursor}, framesize: {matchFrameData.Frame.GetPhysicalSizeInMegabytes():N2}mb",
                e);
        }
    }
}