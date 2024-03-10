using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillIssue.Database;
using SkillIssue.Domain.Unfair.Entities;
using TheGreatSpy.Services;

namespace TheGreatSpy.HostedServices;

public class PlayerStatisticsService(IServiceScopeFactory scopeFactory, ILogger<PlayerStatisticsService> logger)
    : BackgroundService
{
    private static readonly HashSet<string> IgnoreList = ["VA", "AQ"];
    private static readonly TimeSpan WaitingTime = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                var countries = await database.Players
                    .GroupBy(x => x.CountryCode)
                    .Where(x => !IgnoreList.Contains(x.Key))
                    .OrderByDescending(x => x.Count())
                    .Select(x => x.Key).ToListAsync(stoppingToken);

                var playerService = scope.ServiceProvider.GetRequiredService<PlayerService>();
                logger.LogInformation("Country leaderboard update started");
                foreach (var country in countries) await FetchCountry(country, playerService, stoppingToken);

                var notUpdatedPlayers = await database.Ratings
                    .AsNoTracking()
                    .Where(x => x.RatingAttributeId == 0)
                    .Select(x => x.Player)
                    .Where(x => x.LastUpdated.AddDays(1) < DateTime.UtcNow)
                    .Select(x => new
                    {
                        x.PlayerId,
                        x.ActiveUsername
                    })
                    .ToListAsync(stoppingToken);

                logger.LogInformation("Player update started with {Amount} of players to update",
                    notUpdatedPlayers.Count);
                foreach (var player in notUpdatedPlayers)
                {
                    logger.LogInformation("{Method}({PlayerId}) = {PlayerUsername}",
                        nameof(playerService.UpdatePlayerById),
                        player.PlayerId,
                        player.ActiveUsername);
                    await playerService.UpdatePlayerById(player.PlayerId, stoppingToken);
                }

                logger.LogInformation("{Service} completed the cycle, waiting for {Minutes}",
                    nameof(PlayerStatisticsService), WaitingTime.TotalMinutes);
            }

            await Task.Delay(WaitingTime, stoppingToken);
        }
    }

    private async Task FetchCountry(string country, PlayerService playerService, CancellationToken token)
    {
        var page = 1;
        const int pageSize = 50;
        do
        {
            JsonObject prefetch;
            try
            {
                logger.LogInformation("Updating country {CountryCode} on page {Page}", country, page);
                var fab = await playerService.GetLeaderboard(country, page, token);
                if (fab is null) return;
                prefetch = fab;
            }
            catch (Exception ex) when (ex is JsonException or HttpRequestException)
            {
                logger.LogWarning(ex, "Country {CountryCode} handles an exception", country);
                return;
            }


            var cursor = prefetch["cursor"]?.AsObject();

            var rankings = prefetch["ranking"]!.AsArray();

            List<Player> players = [];

            var xx = (page - 1) * pageSize;
            var now = DateTime.UtcNow;
            foreach (var ranking in rankings)
            {
                var user = ranking!["user"];
                players.Add(new Player
                {
                    PlayerId = user!["id"].Deserialize<int>(),
                    ActiveUsername = user["username"].Deserialize<string>()!,
                    CountryCode = user["country_code"].Deserialize<string>()!,
                    AvatarUrl = user["avatar_url"]!.Deserialize<string>()!,
                    GlobalRank = ranking["global_rank"]?.Deserialize<int>(),
                    CountryRank = ++xx,
                    Pp = ranking["pp"]?.Deserialize<double>(),
                    IsRestricted = false,
                    LastUpdated = now
                });
            }

            if (players.FirstOrDefault()?.GlobalRank > 999999) return;
            await playerService.UpsertPlayers(players, withStatistics: true);

            if (cursor is null || rankings.Count < pageSize) return;
            page = cursor["page"].Deserialize<int>();
        } while (page <= 200);
    }
}