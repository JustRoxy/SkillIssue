using EasyNetQ;
using MediatR;
using SkillIssue.Common.Contracts.Matches;
using SkillIssue.Matches.Contracts;
using SkillIssue.Matches.Database;
using SkillIssue.Matches.Queries.GetPageFromAPI;

namespace SkillIssue.Matches.Services;

public class BackgroundPageUpdater(IServiceScopeFactory scopeFactory, ILogger<BackgroundPageUpdater> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var matchesExist = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var mediatr = scope.ServiceProvider.GetRequiredService<IMediator>();
            var repository = scope.ServiceProvider.GetRequiredService<MongoMatchesRepository>();
            matchesExist = matchesExist || await MatchesExist(repository, stoppingToken);

            var pageCursor = matchesExist ? await GetPageCursor(repository, stoppingToken) : 0;
            var page = await mediatr.Send(new GetPageFromAPIRequest
            {
                Cursor = pageCursor
            }, stoppingToken);

            if (page.Matches.Count == 0)
            {
                await NoNewMatches(stoppingToken);
                continue;
            }

            var matches = CreateMatches(page);
            await SaveMatches(repository, matches, stoppingToken);
            await PublishMessage(scope.ServiceProvider.GetRequiredService<IBus>(), matches);
        }
    }

    private async Task NoNewMatches(CancellationToken stoppingToken)
    {
        logger.LogInformation("Page is empty. Waiting for 15 seconds and skipping");

        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
    }

    private static async Task PublishMessage(IBus bus, List<MatchResponse> newMatches)
    {
        var message = new NewMatchesEvent()
        {
            NewMatches = newMatches.Select(match => new NewMatchesEvent.NewMatch
            {
                MatchId = match.MatchId,
                Name = match.MatchInfo.Name,
                StartTime = match.MatchInfo.StartTime,
                IsTournamentGame = match.IsNameInTournamentFormat,
            }).ToList()
        };

        await bus.PubSub.PublishAsync(message);
    }

    private static List<MatchResponse> CreateMatches(Page page)
    {
        return page.Matches.Select(x => new MatchResponse()
            {
                MatchInfo = new MatchInfo
                {
                    MatchId = x.MatchId,
                    StartTime = x.StartTime,
                    EndTime = null,
                    Name = x.Name
                }
            })
            .ToList();
    }

    private async Task SaveMatches(MongoMatchesRepository repository, List<MatchResponse> matches, CancellationToken cancellationToken)
    {
        try
        {
            await repository.InsertManyAsync(matches, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "!!!! Failed to save matches");
        }
    }

    private static async Task<bool> MatchesExist(MongoMatchesRepository repository, CancellationToken cancellationToken)
    {
        var matchesExist = await repository.ExistsAnyAsync(cancellationToken);
        return matchesExist;
    }

    private static async Task<long> GetPageCursor(MongoMatchesRepository repository, CancellationToken cancellationToken)
    {
        var pageCursor = await repository.GetMaxIdAsync(cancellationToken);
        return pageCursor;
    }
}