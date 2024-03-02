using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkillIssue.Database;
using SkillIssue.Domain.Unfair.Entities;
using TheGreatSpy.Services;

namespace TheGreatSpy;

public class AlphaMigration(IServiceScopeFactory serviceScopeFactory, ILogger<AlphaMigration> logger)
{
    public async Task MigrateFromTgml()
    {
        await using var globalScope = serviceScopeFactory.CreateAsyncScope();
        await using var globalContext = globalScope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var total = await globalContext.TgmlMatches.CountAsync();

        var matches = await globalContext.TgmlMatches.AsNoTracking()
            .Where(x => x.CompressedJson != null)
            .OrderBy(x => x.MatchId).ToListAsync();
        var xx = 0;


        ConcurrentDictionary<int, Player> players = [];
        await Parallel.ForEachAsync(matches, async (match, token) =>
        {
            logger.LogInformation("Processing: {Amount} / {Total}", ++xx, total);

            var content = await match.Deserialize();

            var playerList = content!["users"]!.AsArray()
                .Select(x => new Player
                {
                    PlayerId = x!["id"].Deserialize<int>(),
                    ActiveUsername = x["username"].Deserialize<string>()!,
                    CountryCode = x["country_code"].Deserialize<string>()!,
                    AvatarUrl = x["avatar_url"].Deserialize<string>()!
                }).ToList();

            playerList.ForEach(x => players[x.PlayerId] = x);
        });

        globalContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(30));
        var playerService = globalScope.ServiceProvider.GetRequiredService<PlayerService>();
        await playerService.UpsertPlayers(players.Values);
    }
}