using MediatR;
using Microsoft.Extensions.Logging;
using SkillIssue.Application.Commands.Stage2UpdateInProgressMatches.Contracts;
using SkillIssue.Common;
using SkillIssue.Common.Exceptions;
using SkillIssue.Domain;
using SkillIssue.Repository;
using SkillIssue.ThirdParty.API.Osu;
using SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Bugs;
using SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts;
using SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts.Match;

namespace SkillIssue.Application.Commands.Stage2UpdateInProgressMatches;

public class UpdateInProgressMatchesHandler(
    ILogger<UpdateInProgressMatchesHandler> logger,
    IMatchRepository matchRepository,
    IMatchFrameRepository matchFrameRepository,
    IOsuClientFactory osuClientFactory)
    : IRequestHandler<UpdateInProgressMatchesRequest>
{
    //TODO: add logging
    private readonly ILogger<UpdateInProgressMatchesHandler> _logger = logger;
    private readonly IOsuClient _osuClient = osuClientFactory.CreateClient(OsuClientType.Types.TGML_CLIENT);

    public async Task Handle(UpdateInProgressMatchesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var matches = await GetMatchesToUpdate(cancellationToken);
            ValidateMatchesInCorrectStatus(matches);
            await UpdateMatches(matches, cancellationToken);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to update matches in {nameof(UpdateInProgressMatchesHandler)}", e);
        }
    }

    private async Task<List<Match>> GetMatchesToUpdate(CancellationToken cancellationToken)
    {
        try
        {
            var matches =
                await matchRepository.FindMatchesInStatus(Match.Status.InProgress, 1000, true, cancellationToken);

            return matches.ToList();
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to fetch matches in status {Match.Status.InProgress}", e);
        }
    }

    /// <summary>
    ///     With this architecure we collect frames of the match in `match_frame` table, and we imply that we can decide on the status transition only by last frame
    /// </summary>
    private Task ValidateAndMoveMatchIfValid(Match match, CancellationToken cancellationToken)
    {
        if (!MatchHasEnded(match))
            return Task.CompletedTask;

        return matchRepository.ChangeMatchStatusToCompleted(match.MatchId, GetEndTime(match), cancellationToken);
    }

    private DateTimeOffset GetEndTime(Match match) => match.EndTime ??
                                                      throw new SeriousValidationException(
                                                          $"expected datetime of the `lastFrame` to be populated. matchId: {match.MatchId}, endTime: {match.EndTime}");

    private void ValidateMatchesInCorrectStatus(IEnumerable<Match> matches)
    {
        foreach (var match in matches)
        {
            if (match.MatchStatus != Match.Status.InProgress)
                throw new Exception(
                    $"Found match in incorrect status, expected {Match.Status.InProgress}. id: {match.MatchId}, status: {match.MatchStatus}");
        }
    }

    /// <summary>
    ///    Requires <see cref="Match"/> to be constantly up-to-date.
    /// </summary>
    private bool MatchHasEnded(Match match)
    {
        var infiniteTimeBugHappened = InfiniteMatchesBug.IsMatchInfinite(match.LastEventTimestamp);
        if (infiniteTimeBugHappened) match.EndTime = match.LastEventTimestamp;

        return match.EndTime is not null;
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
        var lastFrame = await EnumerateMatchUpdates(match, cancellationToken);
        if (lastFrame is null) return;

        await ValidateAndMoveMatchIfValid(match, cancellationToken);
    }

    private async Task<MatchFrame?> EnumerateMatchUpdates(Match match, CancellationToken cancellationToken)
    {
        var request = new GetMatchRequest()
        {
            Cursor = match.Cursor,
            MatchId = match.MatchId
        };

        var updateEnumerable = _osuClient.GetMatchAsAsyncEnumerable(request, cancellationToken);
        MatchFrame? lastFrame = null;
        await foreach (var frame in updateEnumerable)
        {
            await HandleFrame(match, frame, cancellationToken);
            lastFrame = frame.Representation;
        }

        return lastFrame;
    }

    private async Task HandleFrame(Match match, MatchFrameRaw frame, CancellationToken cancellationToken)
    {
        var compressedFrame = await CompressMatchFrame(match, frame, cancellationToken);

        var frameData = new MatchFrameData
        {
            MatchId = match.MatchId,
            Cursor = frame.Cursor,
            Frame = compressedFrame
        };
        await matchFrameRepository.CacheFrame(frameData, cancellationToken);

        match.Cursor = frame.Cursor;
        match.EndTime = frame.Representation!.MatchInfo.EndTime;
        if (frame.LastEventTimestamp is not null) match.LastEventTimestamp = frame.LastEventTimestamp.Value;
        await matchRepository.UpdateMatchCursorWithLastTimestamp(match.MatchId,
            match.Cursor ?? throw new SeriousValidationException("expected frame `cursor` to be populated"),
            match.LastEventTimestamp,
            cancellationToken);
    }

    private async Task<byte[]> CompressMatchFrame(Match match, MatchFrameRaw frame, CancellationToken cancellationToken)
    {
        if (frame.RawData is null || frame.RawData.Length == 0)
            throw new Exception($"Found null frame for match {match.MatchId}");

        var compressionLevel = frame.RawData.SuitableBrotliCompressionLevel();
        try
        {
            return await frame.RawData.BrotliCompress(compressionLevel, cancellationToken);
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Failed to compress match. size: {frame.RawData.GetPhysicalSizeInMegabytes():N2}mb, compressionLevel: {compressionLevel}",
                e);
        }
    }
}