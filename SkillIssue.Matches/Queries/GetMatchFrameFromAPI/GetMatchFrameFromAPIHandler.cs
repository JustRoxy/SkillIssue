using System.Text.Json;
using MediatR;
using SkillIssue.Common.Types;
using SkillIssue.Matches.Contracts;
using SkillIssue.Matches.Contracts.Events;

namespace SkillIssue.Matches.Queries.GetMatchFrameFromAPI;

public class GetMatchFrameFromAPIHandler(IHttpClientFactory clientFactory) : IRequestHandler<GetMatchFrameFromAPIRequest, GetMatchFrameFromAPIResponse>
{
    private readonly HttpClient _client = clientFactory.CreateClient(Constants.HTTP_CLIENT);

    public async Task<GetMatchFrameFromAPIResponse> Handle(GetMatchFrameFromAPIRequest request, CancellationToken cancellationToken)
    {
        var matchData = await _client.GetByteArrayAsync($"matches/{request.MatchId}?after={request.Cursor}", cancellationToken);

        var match = JsonSerializer.Deserialize<MatchResponse>(matchData)!;

        var response = new GetMatchFrameFromAPIResponse
        {
            Frame = new Frame<MatchResponse>(match, matchData),
            Cursor = FindNextEventIdCursor(match, request.MatchId)
        };

        ValidateCursorIncreased(request.Cursor, response.Cursor);

        response.LastTimestamp = FindLastTimestampUpdate(match);

        return response;
    }


    private static DateTimeOffset? FindLastTimestampUpdate(MatchResponse frame)
    {
        if (frame.Events.Count == 0) return null;

        return frame.Events.MaxBy(ev => ev.EventId)!.Timestamp;
    }

    private void ValidateCursorIncreased(long before, long after)
    {
        if (after < before)
            throw new Exception($"Cursor decreased from {before} to {after}");
    }

    /// <summary>
    ///     We should stop before the ongoing game event
    /// </summary>
    private long FindNextEventIdCursor(MatchResponse match, long previousCursor)
    {
        var maxEvent = FindMaxEventId(match, previousCursor);
        //If there are no ongoing game we are ready to skip this frame
        if (match.CurrentGameId is null) return maxEvent;


        return FindEventBeforeOngoingGame(match, previousCursor);
    }

    private long FindMaxEventId(MatchResponse matchFrame, long defaultValue)
    {
        if (matchFrame.Events.Count == 0) return defaultValue;

        return matchFrame.Events.Max(ev => ev.EventId);
    }

    private long FindEventBeforeOngoingGame(MatchResponse frame, long defaultValue)
    {
        //If there are no events at the frame it means either API returned an empty response, or we hit the end on the match events
        //We assume the positive result: we hit the end of the match events
        if (frame.Events.Count == 0) return defaultValue;

        //If there are only one event - then check if it's an ongoing game or not.
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

    private static bool EventIsOngoingGame(MatchResponse frame, MatchEvent @event)
    {
        return @event.Game?.Id == frame.CurrentGameId;
    }
}