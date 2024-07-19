using MediatR;
using Microsoft.Extensions.Logging;
using SkillIssue.Application.Commands.Stage1FindNewMatches.Contracts;
using SkillIssue.Application.Services.IsTournamentMatch;
using SkillIssue.Domain;
using SkillIssue.Repository;
using SkillIssue.ThirdParty.Osu;

namespace SkillIssue.Application.Commands.Stage1FindNewMatches;

public class FindNewMatchesHandler(
    IOsuClientFactory osuClientFactory,
    IMatchRepository repository,
    IIsTournamentMatch isTournamentMatch,
    ILogger<FindNewMatchesHandler> logger)
    : IRequestHandler<FindNewMatchesRequest>
{
    private const int MAX_REPEAT_AMOUNT = 100;
    private readonly IOsuClient _osuClient = osuClientFactory.CreateClient(OsuClientType.Types.TGML_CLIENT);

    public async Task Handle(FindNewMatchesRequest request, CancellationToken cancellationToken)
    {
        for (var i = 0; i < MAX_REPEAT_AMOUNT; i++)
        {
            var hasNext = await HandleInner(cancellationToken);
            if (!hasNext)
            {
                logger.LogInformation("No more new matches, quiting early");
                return;
            }
        }
    }

    private async Task<bool> HandleInner(CancellationToken cancellationToken)
    {
        try
        {
            var matches = await FindNewMatches(cancellationToken);
            SetInProgressStatus(matches);
            MarkIsTournamentMatch(matches);
            await SaveNewMatches(matches, cancellationToken);

            return matches.Count != 0;
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to save new match page for {nameof(FindNewMatchesHandler)}", e);
        }
    }

    private async Task<List<Match>> FindNewMatches(CancellationToken cancellationToken)
    {
        var lastMatch = await repository.FindLastMatchId(cancellationToken);
        var page = await _osuClient.GetNextMatchPage(lastMatch ?? 0, cancellationToken);
        return page.Matches.Select(match => match.ToDomain()).ToList();
    }

    private void MarkIsTournamentMatch(IEnumerable<Match> matches)
    {
        foreach (var match in matches)
        {
            match.IsTournament = isTournamentMatch.IsTournamentMatch(match);
        }
    }

    private void SetInProgressStatus(IEnumerable<Match> matches)
    {
        foreach (var match in matches)
        {
            match.MatchStatus = Match.Status.InProgress;
        }
    }

    private async Task SaveNewMatches(IReadOnlyList<Match> matches, CancellationToken cancellationToken)
    {
        try
        {
            await repository.UpsertMatchesWithBulk(matches, cancellationToken);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to save {matches.Count} new matches", e);
        }
    }
}