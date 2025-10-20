using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillIssue.Database;
using SkillIssue.Domain.Events.Matches;

namespace TheGreatMultiplayerLibrary.Services;

public class TheGreatArchiving(
    IServiceScopeFactory scopeFactory,
    ILogger<TheGreatArchiving> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await using var databaseContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var lastMatch = await databaseContext
                .TgmlMatches
                .MaxAsync(x => (int?)x.MatchId, stoppingToken) ?? 0;
            var page = await scope.ServiceProvider.GetRequiredService<TheGreatArchiver>().GetPage(lastMatch);
            if (page is null)
            {
                logger.LogCritical("Got null from page starting from {LastMatch}", lastMatch);
                throw new Exception();
            }

            if (page.Count == 0)
            {
                logger.LogInformation("TheGreatArchiving is up to sync. Waiting one minute...");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            foreach (var match in page)
            {
                match.AddDomainEvent(new MatchFound
                {
                    Match = match
                });

                databaseContext.TgmlMatches.Add(match);

                databaseContext.FlowStatus.Add(new FlowStatusTracker
                {
                    MatchId = match.MatchId,
                    Status = FlowStatus.Created
                });
            }

            await databaseContext.SaveChangesAsync(stoppingToken);
        }
    }
}