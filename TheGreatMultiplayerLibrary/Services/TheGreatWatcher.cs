using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillIssue.Database;
using SkillIssue.Domain.Events.Matches;
using SkillIssue.Domain.TGML.Entities;

namespace TheGreatMultiplayerLibrary.Services;

public class TheGreatWatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<TheGreatWatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var notCompletedMatches = await GetOngoingMatches(stoppingToken);

            logger.LogInformation("Received {NotCompletedMatchesCount} not complete matches", notCompletedMatches.Count);

            foreach (var matchId in notCompletedMatches)
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                var archiver = scope.ServiceProvider.GetRequiredService<TheGreatArchiver>();

                var match = await context.TgmlMatches
                    .AsSplitQuery()
                    .Include(x => x.Players)
                    .FirstOrDefaultAsync(x => x.MatchId == matchId, stoppingToken);

                if (match is null)
                {
                    logger.LogCritical("WHAT {MatchId} ???", matchId);
                    continue;
                }

                logger.LogInformation("Processing {MatchName} ({MatchId})", match.Name, match.MatchId);

                JsonObject updated;

                try
                {
                    // get new match root if no data is available, otherwise continue with parsing until completed
                    updated = match.CompressedJson is null ? await GetMatchRoot(match, archiver) : await GetProgressingMatch(match, archiver);
                }
                catch (HttpRequestException exception)
                {
                    logger.LogError(exception, "An HttpRequestException happened :/");

                    if (exception.StatusCode == HttpStatusCode.NotFound)
                    {
                        logger.LogWarning("Match is gone {MatchId}", match.MatchId);
                        match.MatchStatus = TgmlMatchStatus.Gone;
                    }

                    await context.SaveChangesAsync(stoppingToken);
                    continue;
                }

                var timestamp = Stopwatch.GetTimestamp();
                await match.Serialize(updated);
                logger.LogInformation("Compressed match in {TimeStamp} for {MatchId}",
                    Stopwatch.GetElapsedTime(timestamp).TotalSeconds, match.MatchId);

                match.Name = updated["match"]?["name"]?.Deserialize<string>() ??
                             throw new Exception($"No match name for {match.MatchId}");
                var endTime = updated["match"]?["end_time"]?.Deserialize<DateTime?>()?.ToUniversalTime();

                if (endTime is not null)
                {
                    match.EndTime = endTime;
                    match.MatchStatus = TgmlMatchStatus.Completed;
                }
                else
                {
                    var lastEventTime = updated["events"]!.AsArray().Last()!["timestamp"]
                        .Deserialize<DateTime>()
                        .ToUniversalTime();

                    if (DateTime.UtcNow - lastEventTime > TimeSpan.FromHours(2))
                    {
                        match.EndTime = lastEventTime;
                        match.MatchStatus = TgmlMatchStatus.Completed;
                    }
                }

                var players = updated["users"]?.AsArray().Select(x => new TgmlPlayer
                {
                    PlayerId = x!["id"].Deserialize<int>(),
                    CurrentUsername = x["username"].Deserialize<string>()!
                }).ToList();

                var playersIds = players!.Select(x => x.PlayerId).ToList();
                var databasePlayers = await context.TgmlPlayers.Where(x => playersIds.Contains(x.PlayerId))
                    .ToDictionaryAsync(x => x.PlayerId, stoppingToken);

                for (var i = 0; i < players!.Count; i++)
                {
                    var localPlayer = players[i];

                    if (databasePlayers.TryGetValue(localPlayer.PlayerId, out var databasePlayer))
                    {
                        databasePlayer.CurrentUsername = localPlayer.CurrentUsername;
                        players[i] = databasePlayer;
                    }
                    else
                    {
                        context.TgmlPlayers.Add(localPlayer);
                    }
                }

                match.Players.Clear();
                players.ForEach(x => match.Players.Add(x));

                match.AddDomainEvent(new MatchUpdated
                {
                    Match = match,
                    DeserializedMatch = updated
                });

                if (match.MatchStatus == TgmlMatchStatus.Completed)
                {
                    var tracker =
                        await context.FlowStatus.FirstOrDefaultAsync(x => x.MatchId == match.MatchId, stoppingToken);
                    tracker!.Status = FlowStatus.TgmlFetched;

                    match.AddDomainEvent(new MatchCompleted
                    {
                        Match = match,
                        DeserializedMatch = updated
                    });
                }

                await context.SaveChangesAsync(stoppingToken);
            }
        }
    }

    private async Task<List<int>> GetOngoingMatches(CancellationToken stoppingToken)
    {
        await using var globalScope = scopeFactory.CreateAsyncScope();

        var globalContext = globalScope.ServiceProvider.GetRequiredService<DatabaseContext>();

        return await globalContext.TgmlMatches
            .AsNoTracking()
            .Where(x => x.MatchStatus == TgmlMatchStatus.Ongoing)
            .Select(x => x.MatchId)
            .OrderBy(x => x)
            .ToListAsync(stoppingToken);
    }

    private async Task<JsonObject> GetMatchRoot(TgmlMatch match, TheGreatArchiver archiver)
    {
        var root = await archiver.GetMatchRoot(match.MatchId);

        if (root is null)
        {
            logger.LogCritical("Could not fetch match root ({MatchId}) :/", match.MatchId);
            throw new Exception();
        }

        while (archiver.HasMatchNodeAfter(root))
            root = await archiver.GetMatchAfter(match.MatchId, root);

        return root;
    }

    private async Task<JsonObject> GetProgressingMatch(TgmlMatch match, TheGreatArchiver archiver)
    {
        var decompressed = await match.Deserialize() ?? throw new Exception();

        // Probe
        decompressed = await archiver.GetMatchAfterProbe(match.MatchId, decompressed);
        while (archiver.HasMatchNodeAfter(decompressed))
            decompressed = await archiver.GetMatchAfter(match.MatchId, decompressed);

        return decompressed;
    }
}