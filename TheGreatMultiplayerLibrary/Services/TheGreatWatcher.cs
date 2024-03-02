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
            await using var globalScope = scopeFactory.CreateAsyncScope();
            await using var globalContext = globalScope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var archiver = globalScope.ServiceProvider.GetRequiredService<TheGreatArchiver>();

            var notCompletedMatches = globalContext.TgmlMatches
                .AsSplitQuery()
                .Include(x => x.Players)
                .OrderBy(x => x.MatchId)
                .Where(x => x.MatchStatus == TgmlMatchStatus.Ongoing);

            await foreach (var match in notCompletedMatches.AsAsyncEnumerable().WithCancellation(stoppingToken))
            {
                logger.LogInformation("Processing {MatchName} ({MatchId})", match.Name, match.MatchId);
                JsonObject updated;
                try
                {
                    if (match.CompressedJson is null)
                    {
                        var root = await archiver.GetMatchRoot(match.MatchId);
                        if (root is null)
                        {
                            logger.LogCritical("Could not fetch match root ({MatchId}) :/", match.MatchId);
                            throw new Exception();
                        }

                        while (archiver.HasMatchNodeAfter(root))
                            root = await archiver.GetMatchAfter(match.MatchId, root);

                        updated = root;
                    }
                    else
                    {
                        var decompressed = await match.Deserialize() ?? throw new Exception();

                        // Probe
                        decompressed = await archiver.GetMatchAfterProbe(match.MatchId, decompressed);
                        while (archiver.HasMatchNodeAfter(decompressed))
                            decompressed = await archiver.GetMatchAfter(match.MatchId, decompressed);

                        updated = decompressed;
                    }
                }
                catch (HttpRequestException exception)
                {
                    logger.LogError(exception, "An HttpRequestException happened :/");
                    if (exception.StatusCode == HttpStatusCode.NotFound)
                    {
                        match.MatchStatus = TgmlMatchStatus.Gone;
                        await globalContext.SaveChangesAsync(stoppingToken);
                    }

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
                var databasePlayers = await globalContext.TgmlPlayers.Where(x => playersIds.Contains(x.PlayerId))
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
                        globalContext.TgmlPlayers.Add(localPlayer);
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
                        await globalContext.FlowStatus.FindAsync([match.MatchId], stoppingToken);
                    tracker!.Status = FlowStatus.TgmlFetched;

                    match.AddDomainEvent(new MatchCompleted
                    {
                        Match = match,
                        DeserializedMatch = updated
                    });
                }

                await globalContext.SaveChangesAsync(stoppingToken);
                globalContext.ChangeTracker.Clear();
            }
        }
    }
}