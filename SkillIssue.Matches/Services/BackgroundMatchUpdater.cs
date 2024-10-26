using MediatR;
using SkillIssue.Common.Types;
using SkillIssue.Matches.Contracts;
using SkillIssue.Matches.Database;
using SkillIssue.Matches.Extensions;
using SkillIssue.Matches.Queries.GetMatchFrameFromAPI;

namespace SkillIssue.Matches.Services;

public class BackgroundMatchUpdater(IServiceScopeFactory scopeFactory, ILogger<BackgroundMatchUpdater> logger) : BackgroundService
{
    private IMediator _mediator = null!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            _mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var repository = scope.ServiceProvider.GetRequiredService<MongoMatchesRepository>();

            await foreach (var _ in repository.FindOngoingTournamentPrioritizedMatchesAsyncEnumerable(stoppingToken))
            {
                var match = _;

                try
                {
                    var iteration = 0;
                    do
                    {
                        logger.LogInformation("Fetching {Name} with iteration {Iteration}", match.MatchInfo.Name, ++iteration);
                        var nextFrame = await GetNextState(match, stoppingToken);

                        if (nextFrame is null)
                        {
                            logger.LogError("Received null frame. MatchId = {MatchId}", match.MatchId);
                            continue;
                        }

                        //TODO: save frame data
                        var matchMerge = match.Merge(nextFrame.Value);
                        await repository.SaveMatchAsync(matchMerge, stoppingToken);

                        match = matchMerge;

                        //Make 1 request if not ended
                        //Consume 'till the end if ended
                    } while (match.MatchInfo.EndTime is not null && !match.ConsumedAllEvents());
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to update match {MatchId}", match.MatchId);
                }
            }
        }
    }

    private async Task<Frame<MatchResponse>?> GetNextState(MatchResponse previous, CancellationToken cancellationToken)
    {
        var cursor = previous.FindNextEventIdCursor(0);

        var response = await _mediator.Send(new GetMatchFrameFromAPIRequest
        {
            MatchId = previous.MatchId,
            Cursor = cursor
        }, cancellationToken);

        return response.Frame;
    }
}