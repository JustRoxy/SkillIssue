using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Common.Extensions;
using SkillIssue.Common.Types;
using SkillIssue.Matches.Contracts;
using SkillIssue.Matches.Models;
using SkillIssue.Matches.Queries.GetMatchFrameFromAPI;

namespace SkillIssue.Matches.Services;

public class BackgroundMatchUpdater(IServiceScopeFactory scopeFactory, ILogger<BackgroundMatchUpdater> logger) : BackgroundService
{
    private IMediator _mediator = null!;
    private MatchesContext _context = null!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            _mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            _context = scope.ServiceProvider.GetRequiredService<MatchesContext>();

            var ongoingMatches = _context.Matches.Where(x => x.EndTime == null);
            ongoingMatches = PrioritizeTournamentMatches(ongoingMatches);
            ongoingMatches = PrioritizeOldestMatches(ongoingMatches);

            var matches = await ongoingMatches
                .Select(x => new
                {
                    Match = x,
                    LastFrame = x.Frames.LastOrDefault()
                })
                .ToListAsync(cancellationToken: stoppingToken);


            foreach (var matchAggregation in matches)
            {
                var match = matchAggregation.Match;
                var lastFrame = matchAggregation.LastFrame;

                try
                {
                    var nextFrame = await UpdateMatch(match, lastFrame, stoppingToken);

                    if (nextFrame is null)
                    {
                        logger.LogError("Received null frame. MatchId = {MatchId}, lastFrame = {@LastFrame}", match.MatchId, lastFrame);
                        continue;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to update match {MatchId} on frame {@LastFrame}", match.MatchId, lastFrame);
                }
            }
        }
    }

    private async Task<Frame<MatchResponse>?> UpdateMatch(Match match, MatchFrame? frame, CancellationToken cancellationToken)
    {
        var cursor = GetCursor(frame);

        var response = await _mediator.Send(new GetMatchFrameFromAPIRequest
        {
            MatchId = match.MatchId,
            Cursor = cursor
        }, cancellationToken);

        return response?.Frame;
    }

    private long GetCursor(MatchFrame? frame)
    {
        if (frame == null) return 0;

        var decompressed = frame.Data.BrotliDecompress();

        var match = JsonSerializer.Deserialize<MatchResponse>(decompressed)!;
        var ids = match.Events.Select(x => x.EventId).ToList();

        return ids.Count == 0
            ? match.LatestEventId
            : ids.Max();
    }


    private static IQueryable<Match> PrioritizeTournamentMatches(IQueryable<Match> query)
    {
        return query.OrderByDescending(x => x.IsNameInTournamentFormat);
    }

    private static IQueryable<Match> PrioritizeOldestMatches(IQueryable<Match> query)
    {
        return query.OrderBy(x => x.MatchId);
    }
}