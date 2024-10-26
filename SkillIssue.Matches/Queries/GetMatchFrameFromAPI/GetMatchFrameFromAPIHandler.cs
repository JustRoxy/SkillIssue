using System.Text.Json;
using MediatR;
using SkillIssue.Common.Types;
using SkillIssue.Matches.Contracts;
using SkillIssue.Matches.Contracts.Events;
using SkillIssue.Matches.Extensions;

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
            Cursor = match.FindNextEventIdCursor(request.Cursor)
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
}