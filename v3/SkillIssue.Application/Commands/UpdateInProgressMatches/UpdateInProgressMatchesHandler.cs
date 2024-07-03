using System.IO.Compression;
using MediatR;
using Microsoft.Extensions.Logging;
using SkillIssue.Application.Commands.UpdateInProgressMatches.Contracts;
using SkillIssue.Common;
using SkillIssue.Domain;
using SkillIssue.Repository;
using SkillIssue.ThirdParty.Osu;

namespace SkillIssue.Application.Commands.UpdateInProgressMatches;

public class UpdateInProgressMatchesHandler : IRequestHandler<UpdateInProgressMatchesRequest>
{
    private readonly ILogger<UpdateInProgressMatchesHandler> _logger;
    private readonly IMatchRepository _matchRepository;
    private readonly IOsuClient _osuClient;

    public UpdateInProgressMatchesHandler(ILogger<UpdateInProgressMatchesHandler> logger,
        IMatchRepository matchRepository, IOsuClientFactory osuClientFactory)
    {
        _logger = logger;
        _matchRepository = matchRepository;
        _osuClient = osuClientFactory.CreateClient(OsuClientType.Types.TGML_CLIENT);
    }

    public async Task Handle(UpdateInProgressMatchesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var matches = await GetMatchesToUpdate(cancellationToken);
            ValidateMatchesInCorrectStatus(matches);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to update matches for {nameof(UpdateInProgressMatchesHandler)}", e);
        }
    }

    private async Task<IEnumerable<Match>> GetMatchesToUpdate(CancellationToken cancellationToken)
    {
        try
        {
            var matches =
                await _matchRepository.FindMatchesInStatus(Match.Status.InProgress, 1000, true, cancellationToken);

            return matches;
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to fetch matches in status {Match.Status.InProgress}", e);
        }
    }

    private void ValidateMatchesInCorrectStatus(IEnumerable<Match> matches)
    {
        foreach (var match in matches)
        {
            if (match.MatchStatus != Match.Status.InProgress)
                throw new Exception(
                    $"Found match in incorrect status, expected {Match.Status.InProgress}. id: {match.MatchId}, status: {match.MatchStatus}");
        }
    }

    private async Task UpdateMatches(IEnumerable<Match> matches, CancellationToken cancellationToken)
    {
        foreach (var match in matches)
        {
            try
            {
                await UpdateMatch(match, cancellationToken);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to update match. id: {match.MatchId}", e);
            }
        }
    }

    private async Task UpdateMatch(Match match, CancellationToken cancellationToken)
    {
    }

    private async Task CompressMatchContent(Match match)
    {
        if (match.Content is null)
            throw new Exception($"Found null content for match {match.MatchId}");

        var compressionLevel = FindSuitableCompressionLevel(match.Content);
        try
        {
            match.Content = await match.Content.BrotliCompress(compressionLevel);
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Failed to compress match. size: {match.Content.GetPhysicalSizeInMegabytes():N2}mb, compressionLevel: {compressionLevel}",
                e);
        }
    }

    private CompressionLevel FindSuitableCompressionLevel(byte[] content)
    {
        var compressionLevel = content.GetPhysicalSizeInMegabytes() switch
        {
            < 10 => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Optimal
        };

        return compressionLevel;
    }
}