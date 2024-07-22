using System.Text;
using SkillIssue.Common.Exceptions;
using SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts;
using SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts.Match;
using SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts.Match.Events;

namespace SkillIssue.ThirdParty.API.Osu.Queries.GetMatch;

public class GetMatchHandler(HttpClient client, OsuRequestBuilder requestBuilder)
{
    public GetMatchResponse Handle(GetMatchRequest request, CancellationToken cancellationToken)
    {
        return new GetMatchResponse(GetNextMatchFrame(request, cancellationToken),
            HasNextMatchFrameInScope,
            () => ValueTask.CompletedTask
        );
    }

    private Func<MatchFrameRaw, Task<MatchFrameRaw>> GetNextMatchFrame(GetMatchRequest request,
        CancellationToken cancellationToken)
    {
        var currentEventId = request.Cursor ?? 0;
        return async _ =>
        {
            var httpRequest =
                requestBuilder.Create(HttpMethod.Get, $"matches/{request.MatchId}?after={currentEventId}");
            var response = await client.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var rawFrame = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var matchFrame = new MatchFrameRaw(rawFrame, currentEventId);
            if (matchFrame.Representation is null)
                throw new Exception(
                    $"Failed to deserialize match content. matchId: {request.MatchId}, cursor: {request.Cursor}, content: {Encoding.UTF8.GetString(rawFrame)}");
            var nextCursorEventId = FindNextEventIdCursor(matchFrame.Representation, currentEventId);
            ValidateCursorIncreased(currentEventId, nextCursorEventId);

            matchFrame.LastEventTimestamp = FindLastTimestampUpdate(matchFrame.Representation);
            currentEventId = nextCursorEventId;
            return matchFrame;
        };
    }

    private DateTimeOffset? FindLastTimestampUpdate(MatchFrame frame)
    {
        if (frame.Events.Count == 0) return null;

        return frame.Events.MaxBy(ev => ev.EventId)!.Timestamp;
    }

    private void ValidateCursorIncreased(long before, long after)
    {
        if (after < before)
            throw new SeriousValidationException($"cursor decreased from {before} to {after}");
    }

    private bool HasNextMatchFrameInScope(MatchFrameRaw frame)
    {
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        //We have no frame, go update it
        if (frame?.Representation is null) return true;
        if (frame.Representation.Events.Count == 0) return false;

        var firstFrameEvent = frame.Representation.Events[0].EventId;
        var lastFrameEvent = frame.Representation.Events.Last().EventId;

        var nextFrameEvent = FindNextEventIdCursor(frame.Representation, firstFrameEvent);

        //We have consumed the whole match, there are no more updates for now.
        if (nextFrameEvent == lastFrameEvent) return false;


        //We haven't consumed the whole frame yet.
        return nextFrameEvent > firstFrameEvent;
    }

    /// <summary>
    ///     We should stop before the ongoing game event
    /// </summary>
    private long FindNextEventIdCursor(MatchFrame matchFrame, long previousCursor)
    {
        var maxEvent = FindMaxEventId(matchFrame, previousCursor);
        //If there are no ongoing game, then we are ready to skip this frame
        if (matchFrame.CurrentGameId is null) return maxEvent;


        return FindEventBeforeOngoingGame(matchFrame, previousCursor);
    }

    private long FindMaxEventId(MatchFrame matchFrame, long defaultValue)
    {
        if (matchFrame.Events.Count == 0) return defaultValue;
        return matchFrame.Events.Max(ev => ev.EventId);
    }

    private long FindEventBeforeOngoingGame(MatchFrame frame, long defaultValue)
    {
        //If there are no events at the frame it means either API returned an empty response, or we hit the end on the match events
        //We assume the positive result: we hit the end of the match events
        if (frame.Events.Count == 0) return defaultValue;

        //If there are only one event - then check if it's an ongoing game or not.
        //TODO (BUG): change eventId to Game.GameId
        if (frame.Events.Count == 1 && EventIsOngoingGame(frame, frame.Events[0]))
            return defaultValue;

        for (var i = 0; i < frame.Events.Count - 1; i++)
        {
            var current = frame.Events[i].EventId;
            var next = frame.Events[i + 1];

            if (EventIsOngoingGame(frame, next)) return current;
        }

        return FindMaxEventId(frame, defaultValue);
    }

    private bool EventIsOngoingGame(MatchFrame frame, MatchEvent @event)
    {
        return @event.Game?.Id == frame.CurrentGameId;
    }
}