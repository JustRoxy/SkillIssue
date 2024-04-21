using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PlayerPerformanceCalculator.Services;
using SkillIssue.Database;
using SkillIssue.Domain.Events.Matches;
using SkillIssue.Domain.PPC.Entities;
using Unfair;

namespace SkillIssue.Handlers;

public class HandleMainFlow(
    DatabaseContext context,
    BeatmapProcessing beatmapProcessing,
    UnfairContext unfairContext,
    ILogger<HandleMainFlow> logger)
    : INotificationHandler<MatchCompleted>
{
    public async Task Handle(MatchCompleted notification, CancellationToken cancellationToken)
    {
        var jsonObject = notification.DeserializedMatch;

        logger.LogInformation("Handling match {MatchId} ({MatchName})", notification.Match.MatchId,
            notification.Match.Name);
        var beatmapIds = jsonObject!["events"]!
            .AsArray().Where(x => x?["game"]?["beatmap"] is not null)
            .Select(x => x!["game"]!["beatmap"]!["id"].Deserialize<int>())
            .Distinct()
            .ToList();

        var flow = await context.FlowStatus.FindAsync([notification.Match.MatchId],
            cancellationToken);

        if (await context.CalculationErrors.AnyAsync(x => x.MatchId == notification.Match.MatchId,
                cancellationToken))
        {
            logger.LogInformation("Already processed {MatchId} ({MatchName})", notification.Match.MatchId,
                notification.Match.Name);
            flow!.Status = FlowStatus.Done;
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        var processedBeatmaps =
            (await context.Beatmaps
                .AsNoTracking()
                .Where(x => beatmapIds.Contains(x.BeatmapId))
                .Where(x => x.Status != BeatmapStatus.NeedsUpdate)
                .Select(x => x.BeatmapId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        logger.LogInformation("Waiting for {Amount} beatmaps to process", beatmapIds.Count - processedBeatmaps.Count);
        foreach (var notProcessedBeatmap in beatmapIds.Where(x => !processedBeatmaps.Contains(x)))
            await beatmapProcessing.LookupAndProcess(notProcessedBeatmap);

        var calculationResult = await unfairContext.CalculateMatch(notification.Match);

        if (calculationResult.RatingHistories is not null && calculationResult.PlayerHistories is not null)
            flow!.AddDomainEvent(new MatchCalculated
            {
                Match = calculationResult.Match!,
                RatingChanges = calculationResult.RatingHistories,
                PlayerHistories = calculationResult.PlayerHistories
            });

        if (calculationResult.RatingHistories is not null)
            context.RatingHistories.AddRange(calculationResult.RatingHistories);

        if (calculationResult.PlayerHistories is not null)
            context.PlayerHistories.AddRange(calculationResult.PlayerHistories);
        if (calculationResult.Match is not null)
            context.Matches.Add(calculationResult.Match);

        context.CalculationErrors.Add(calculationResult.CalculationError);

        flow!.Status = FlowStatus.Done;

        await context.SaveChangesAsync(cancellationToken);
    }
}