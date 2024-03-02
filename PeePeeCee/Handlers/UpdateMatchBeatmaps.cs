using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PeePeeCee.Services;
using SkillIssue.Database;
using SkillIssue.Domain.Events.Matches;
using SkillIssue.Domain.PPC.Entities;

namespace PeePeeCee.Handlers;

public class UpdateMatchBeatmaps(DatabaseContext context, BeatmapProcessing beatmapProcessing)
    : INotificationHandler<MatchUpdated>
{
    public async Task Handle(MatchUpdated notification, CancellationToken cancellationToken)
    {
        var match = notification.DeserializedMatch;

        var beatmapIds = match!["events"]!
            .AsArray().Where(x => x?["game"]?["beatmap"] is not null)
            .Select(x => x!["game"]!["beatmap"]!["id"].Deserialize<int>())
            .Distinct()
            .ToList();

        var processedBeatmaps =
            (await context.Beatmaps
                .AsNoTracking()
                .Where(x => beatmapIds.Contains(x.BeatmapId))
                .Where(x => x.Status != BeatmapStatus.NeedsUpdate)
                .Select(x => x.BeatmapId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var notProcessedBeatmap in beatmapIds.Where(x => !processedBeatmaps.Contains(x)))
            await beatmapProcessing.LookupAndProcess(notProcessedBeatmap);
    }
}