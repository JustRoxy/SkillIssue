using EasyNetQ;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Common.Contracts.Matches;
using SkillIssue.Matches.Contracts;
using SkillIssue.Matches.Models;
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
            var context = scope.ServiceProvider.GetRequiredService<MatchesContext>();
            matchesExist = matchesExist || await MatchesExist(context);

            var pageCursor = matchesExist ? await GetPageCursor(context) : 0;
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
            await SaveMatches(context, matches, stoppingToken);
            await PublishMessage(scope.ServiceProvider.GetRequiredService<IBus>(), matches);
        }
    }

    private async Task NoNewMatches(CancellationToken stoppingToken)
    {
        logger.LogInformation("Page is empty. Waiting for 15 seconds and skipping.");

        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
    }

    private static async Task PublishMessage(IBus bus, List<Match> newMatches)
    {
        var message = new NewMatchesEvent()
        {
            NewMatches = newMatches.Select(match => new NewMatchesEvent.NewMatch()
            {
                MatchId = match.MatchId,
                Name = match.Name,
                StartTime = match.StartTime,
            }).ToList()
        };

        await bus.PubSub.PublishAsync(message);
    }

    private static List<Match> CreateMatches(Page page)
    {
        return page.Matches.Select(x => new Match
            {
                MatchId = x.MatchId,
                Name = x.Name,
                StartTime = x.StartTime,
                EndTime = x.EndTime
            })
            .ToList();
    }

    private async Task SaveMatches(MatchesContext context, List<Match> matches, CancellationToken cancellationToken)
    {
        try
        {
            context.Matches.AddRange(matches);

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "!!!! Failed to save matches");
        }
    }

    private static async Task<bool> MatchesExist(MatchesContext context)
    {
        var matchesExist = await context.Matches.AnyAsync();
        return matchesExist;
    }

    private static async Task<long> GetPageCursor(MatchesContext context)
    {
        var pageCursor = await context.Matches.Select(x => x.MatchId).MaxAsync();
        return pageCursor;
    }
}