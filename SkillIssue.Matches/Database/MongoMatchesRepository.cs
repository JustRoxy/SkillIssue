using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SkillIssue.Common.Database;
using SkillIssue.Matches.Contracts;

namespace SkillIssue.Matches.Database;

public class MongoMatchesRepository(IMongoClient mongoClient) : IRepositoryInitialize
{
    public const string DATABASE_NAME = "SkillIssue_Matches";

    public const string MATCHES_COLLECTION = "Matches";

    public async Task Initialize()
    {
        await Index(GetDefaultCollection());
    }

    public Task<bool> ExistsAnyAsync(CancellationToken cancellationToken)
    {
        return GetDefaultCollection()
            .AsQueryable()
            .AnyAsync(cancellationToken);
    }

    public Task<int> GetMaxIdAsync(CancellationToken cancellationToken)
    {
        return GetDefaultCollection()
            .AsQueryable()
            .Select(x => x.MatchId)
            .MaxAsync(cancellationToken);
    }

    public IAsyncEnumerable<MatchResponse> FindOngoingTournamentPrioritizedMatchesAsyncEnumerable(CancellationToken cancellationToken)
    {
        return GetDefaultCollection()
            .AsQueryable()
            .Where(x => x.MatchInfo.EndTime == null)
            .OrderBy(x => x.IsNameInTournamentFormat)
            .ThenBy(x => x.MatchId)
            .ToAsyncEnumerable(cancellationToken);
    }

    public async Task<MatchResponse?> GetMatchOrDefaultAsync(int id, CancellationToken cancellationToken)
    {
        return await GetDefaultCollection()
            .AsQueryable()
            .Where(match => match.MatchId == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task SaveMatchAsync(MatchResponse response, CancellationToken cancellationToken)
    {
        return GetDefaultCollection()
            .ReplaceOneAsync(x => x.MatchId == response.MatchId,
                response,
                new ReplaceOptions()
                {
                    IsUpsert = true
                }, cancellationToken);
    }

    public Task InsertManyAsync(List<MatchResponse> response, CancellationToken cancellationToken)
    {
        return GetDefaultCollection().InsertManyAsync(response, cancellationToken: cancellationToken);
    }

    private IMongoCollection<MatchResponse> GetDefaultCollection()
    {
        return mongoClient.GetDatabase(DATABASE_NAME).GetCollection<MatchResponse>(MATCHES_COLLECTION);
    }

    private async Task Index(IMongoCollection<MatchResponse> collection)
    {
        var createdIndexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        var indexNames = createdIndexes
            .SelectMany(i => i.Elements)
            .Where(e => string.Equals(e.Name, "name", StringComparison.CurrentCultureIgnoreCase))
            .Select(n => n.Value.ToString())
            .ToHashSet();

        var indexes = new Dictionary<string, CreateIndexModel<MatchResponse>>()
        {
            ["OngoingMatches"] = new(Builders<MatchResponse>.IndexKeys
                    .Ascending(x => x.MatchId)
                    .Ascending(x => x.MatchInfo.EndTime),
                new CreateIndexOptions<MatchResponse>
                {
                    Unique = false,
                    PartialFilterExpression = Builders<MatchResponse>.Filter.Eq(x => x.MatchInfo.EndTime, null)
                }
            )
        };

        foreach (var (key, model) in indexes.Where(x => !indexNames.Contains(x.Key)))
        {
            model.Options.Name = key;
            await collection.Indexes.CreateOneAsync(model);
        }
    }
}