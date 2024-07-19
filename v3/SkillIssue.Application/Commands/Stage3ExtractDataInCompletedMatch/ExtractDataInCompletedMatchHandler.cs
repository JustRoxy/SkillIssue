using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.Extensions.Logging;
using SkillIssue.Application.Commands.Stage3ExtractDataInCompletedMatch.Contracts;
using SkillIssue.Application.Services.MatchData;
using SkillIssue.Common;
using SkillIssue.Common.Exceptions;
using SkillIssue.Common.Utils;
using SkillIssue.Domain;
using SkillIssue.Repository;
using SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts;
using SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts.Match;

namespace SkillIssue.Application.Commands.Stage3ExtractDataInCompletedMatch;

public class ExtractDataInCompletedMatchHandler : IRequestHandler<ExtractDataInCompletedMatchRequest>
{
    private readonly IEnumerable<IMatchDataExtractor> _matchDataExtractors;
    private readonly IMatchRepository _matchRepository;
    private readonly IMatchFrameRepository _matchFrameRepository;
    private readonly ILogger<ExtractDataInCompletedMatchHandler> _logger;

    public ExtractDataInCompletedMatchHandler(IEnumerable<IMatchDataExtractor> matchDataExtractors,
        IMatchRepository matchRepository,
        IMatchFrameRepository matchFrameRepository,
        ILogger<ExtractDataInCompletedMatchHandler> logger)
    {
        _matchDataExtractors = matchDataExtractors;
        _matchRepository = matchRepository;
        _matchFrameRepository = matchFrameRepository;
        _logger = logger;
    }

    public async Task Handle(ExtractDataInCompletedMatchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var matches = await GetMatchesInCompletedStatus(cancellationToken);
            ValidateMatchesInCorrectStatus(matches);
            var matchesFrames = await GetMatchesFrameData(matches, cancellationToken);
            var frameGroups = GroupByMatchId(matchesFrames);
            var mergedFrames =
                await ConvertToMatchFrames(frameGroups, cancellationToken).ToListAsync(cancellationToken);

            mergedFrames = ValidateFramesEventsAreSequential(mergedFrames).ToList();
            await ExtractDataFromFrames(mergedFrames, cancellationToken);
            await MoveMatchesInExtractedStatus(matches, cancellationToken);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to handle {nameof(ExtractDataInCompletedMatchHandler)}", e);
        }
    }

    private async Task ExtractDataFromFrames(List<MatchFrame> mergedFrames, CancellationToken cancellationToken)
    {
        foreach (var matchDataExtractor in _matchDataExtractors)
        {
            try
            {
                await matchDataExtractor.ExtractData(mergedFrames, cancellationToken);
            }
            catch (Exception e)
            {
                throw new SeriousValidationException(
                    $"failed to extract data from matchDataExtractor. name: {matchDataExtractor.GetUnderlyingTypeName()}",
                    e);
            }
        }
    }

    private IEnumerable<MatchFrame> ValidateFramesEventsAreSequential(IEnumerable<MatchFrame> frames)
    {
        foreach (var frame in frames)
        {
            var events = frame.Events;
            bool foundNonSequentialEvent = false;
            for (int i = 0; i < frame.Events.Count - 1; i++)
            {
                var current = events[i].EventId;
                var next = events[i + 1].EventId;
                if (current < next)
                    continue;

                foundNonSequentialEvent = true;
                _logger.LogCritical(
                    "Found non-sequential frame. matchId: {MatchId}, currentEventId: {CurrentFrameId}, nextEventId: {NextEventId}",
                    frame.MatchInfo.MatchId, current, next);
                break;
            }

            if (!foundNonSequentialEvent) yield return frame;
        }
    }

    private async Task<List<Match>> GetMatchesInCompletedStatus(CancellationToken cancellationToken)
    {
        var matches = await _matchRepository.FindMatchesInStatus(Match.Status.Completed, 1000, true, cancellationToken);

        return matches.ToList();
    }

    private void ValidateMatchesInCorrectStatus(IEnumerable<Match> matches)
    {
        foreach (var match in matches)
        {
            if (match.MatchStatus != Match.Status.Completed)
                throw new Exception(
                    $"Found match in incorrect status, expected {Match.Status.Completed}. id: {match.MatchId}, status: {match.MatchStatus}");
        }
    }

    private async Task MoveMatchesInExtractedStatus(IEnumerable<Match> matches, CancellationToken cancellationToken)
    {
        await TimeMeasuring.MeasureAsync(_logger, nameof(MoveMatchesInExtractedStatus), async () =>
        {
            foreach (var match in matches)
            {
                await _matchRepository.ChangeMatchStatus(match.MatchId, Match.Status.DataExtracted, cancellationToken);
            }
        });
    }

    private async Task<IEnumerable<MatchFrameData>> GetMatchesFrameData(IEnumerable<Match> matches,
        CancellationToken cancellationToken)
    {
        var matchIds = matches.Select(match => match.MatchId).ToList();
        return await _matchFrameRepository.GetMatchFramesWithBulk(matchIds, cancellationToken);
    }

    private IEnumerable<IGrouping<int, MatchFrameData>> GroupByMatchId(IEnumerable<MatchFrameData> frameDatas)
    {
        return frameDatas.GroupBy(frame => frame.MatchId);
    }

    private async IAsyncEnumerable<MatchFrame> ConvertToMatchFrames(
        IEnumerable<IGrouping<int, MatchFrameData>> groupedFrames,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var matchFrames in groupedFrames)
        {
            var orderedFrames = matchFrames.OrderBy(frame => frame.Cursor);

            List<MatchFrame> decompressedFrames = [];
            foreach (var frameData in orderedFrames)
            {
                var rawFrame = await ToRawFrame(frameData, cancellationToken);

                var frame = rawFrame.Frame;
                if (frame is null)
                    throw new SeriousValidationException(
                        $"expected `Frame` to be populated. matchId: {frameData.MatchId}, cursor: {frameData.Cursor}, decompressed: {rawFrame.RawFrame.GetPhysicalSizeInMegabytes():N2}mb");
                decompressedFrames.Add(frame);
            }

            var mergedFrame = decompressedFrames.Aggregate(MatchFrame.Merge);
            yield return mergedFrame;
        }
    }

    private async Task<MatchFrameRaw> ToRawFrame(MatchFrameData matchFrameData, CancellationToken cancellationToken)
    {
        try
        {
            var decompressedFrame = await matchFrameData.Frame.BrotliDecompress(cancellationToken);

            return new MatchFrameRaw(decompressedFrame, matchFrameData.Cursor);
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Failed to decompress frame. matchId: {matchFrameData.MatchId}, cursor: {matchFrameData.Cursor}", e);
        }
    }
}