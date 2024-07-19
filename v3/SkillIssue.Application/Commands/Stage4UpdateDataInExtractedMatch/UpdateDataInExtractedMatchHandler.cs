using MediatR;
using Microsoft.Extensions.Logging;
using SkillIssue.Application.Commands.Stage4UpdateDataInExtractedMatch.Contracts;
using SkillIssue.Application.Services.MatchData;
using SkillIssue.Common;
using SkillIssue.Domain;
using SkillIssue.Repository;

namespace SkillIssue.Application.Commands.Stage4UpdateDataInExtractedMatch;

public class UpdateDataInExtractedMatchHandler : IRequestHandler<UpdateDataInExtractedMatchRequest>
{
    private readonly IEnumerable<IMatchDataExtractor> _dataExtractors;
    private readonly IMatchRepository _matchRepository;
    private readonly ILogger<UpdateDataInExtractedMatchHandler> _logger;

    public UpdateDataInExtractedMatchHandler(IEnumerable<IMatchDataExtractor> dataExtractors,
        IMatchRepository matchRepository,
        ILogger<UpdateDataInExtractedMatchHandler> logger)
    {
        _dataExtractors = dataExtractors;
        _matchRepository = matchRepository;
        _logger = logger;
    }

    public async Task Handle(UpdateDataInExtractedMatchRequest request, CancellationToken cancellationToken)
    {
        var matches =
            await _matchRepository.FindMatchesInStatus(Match.Status.DataExtracted, 1000, true, cancellationToken);
        foreach (var match in matches.WithProgressLogging(_logger,
                     $"{nameof(UpdateDataInExtractedMatchHandler)}.{nameof(matches)}"))
        {
            await _dataExtractors.First().UpdateData(match.MatchId, cancellationToken);
        }
    }
}