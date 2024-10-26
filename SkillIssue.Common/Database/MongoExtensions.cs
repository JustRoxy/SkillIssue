using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SkillIssue.Common.Database;

public static class MongoExtensions
{
    public static IServiceCollection RegisterMongo(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("Unable to find required `MongoDB` connection string");
        var client = new MongoClient(connectionString);

        return services.AddSingleton<IMongoClient, MongoClient>(_ => client);
    }

    public static IServiceCollection RegisterMongoRepository<T>(this IServiceCollection services) where T : class, IRepositoryInitialize
    {
        return services.AddSingleton<T>();
    }

    public static async Task<IServiceProvider> CreateCompressedCollections(this IServiceProvider serviceProvider,
        string databaseName, params string[] names)
    {
        var client = serviceProvider.GetRequiredService<IMongoClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<MongoClient>>();
        var database = client.GetDatabase(databaseName);
        var existingCollections = await (await database.ListCollectionNamesAsync()).ToListAsync();
        foreach (var name in names)
        {
            if (existingCollections.Contains(name))
            {
                logger.LogInformation("Compressed Collection {Name} already exist", name);
                continue;
            }

            await database.CreateCollectionAsync(name, new CreateCollectionOptions()
            {
                StorageEngine = new BsonDocument(new Dictionary<string, object>()
                {
                    ["wiredTiger"] = new Dictionary<string, string>()
                    {
                        ["configString"] = "block_compressor=zstd"
                    }
                })
            });

            logger.LogInformation("Compressed Collection {Name} successfuly created", name);
        }

        return serviceProvider;
    }

    public static async Task InitializeRepository<T>(this IServiceProvider serviceProvider) where T : IRepositoryInitialize
    {
        var logger = serviceProvider.GetRequiredService<ILogger<IMongoClient>>();
        var repository = serviceProvider.GetRequiredService<T>();
        logger.LogInformation("Initializing repository {RepositoryName}", repository.GetType().Name);
        await repository.Initialize();
    }

    public static async IAsyncEnumerable<TDocument> ToAsyncEnumerable<TDocument>(
        this IAsyncCursorSource<TDocument> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var cursor = await source.ToCursorAsync(cancellationToken).ConfigureAwait(false);

        await foreach (var document in cursor.ToAsyncEnumerable(cancellationToken))
        {
            yield return document;
        }
    }

    /// <summary>
    /// Provides asynchronous iteration over all document returned by <paramref name="source"/>.
    /// </summary>
    /// <typeparam name="TDocument">The type of the documents to be iterated.</typeparam>
    /// <param name="source">The source cursor.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Async-enumerable that iterates over all document returned by cursor.</returns>
    public static async IAsyncEnumerable<TDocument> ToAsyncEnumerable<TDocument>(
        this IAsyncCursor<TDocument> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await source.MoveNextAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (var document in source.Current)
            {
                yield return document;
            }
        }
    }
}