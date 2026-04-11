// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using MediatR;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using TheGreatSpy.Services;

namespace SkillIssue.API.Commands.Compliance;

public class LookupRatingsOnTimestampRequest : IRequest<LookupRatingsOnTimestampResponse>
{
    public required int[] UserIds { get; init; }

    public required DateTime Timestamp { get; init; }
}

public class LookupRatingsOnTimestampResponse
{
    public class ResponseRating
    {
        public required int UserId { get; set; }
        public required string? Username { get; set; }
        public required int Rating { get; set; }
        public required DateTime? LastUpdateTime { get; set; }
    }

    public required IAsyncEnumerable<ResponseRating> RatingStream { get; init; }
}

public class LookupRatingsOnTimestampHandler : IRequestHandler<LookupRatingsOnTimestampRequest, LookupRatingsOnTimestampResponse>
{
    private readonly DatabaseContext _context;
    private readonly PlayerService _playerService;
    public LookupRatingsOnTimestampHandler(DatabaseContext context, PlayerService playerService)
    {
        _context = context;
        _playerService = playerService;
    }

    public async Task<LookupRatingsOnTimestampResponse> Handle(LookupRatingsOnTimestampRequest request, CancellationToken cancellationToken)
    {
        var requestTimestamp = DateTime.SpecifyKind(request.Timestamp.Date, DateTimeKind.Utc);
        var lastMatchAtDate = await _context.Matches.Where(x => x.EndTime.Date <= requestTimestamp)
            .Select(x => x.MatchId)
            .OrderByDescending(x => x)
            .FirstAsync(cancellationToken: cancellationToken);

        return new LookupRatingsOnTimestampResponse
        {
            RatingStream = GenerateResponses(request.UserIds, lastMatchAtDate, requestTimestamp)
        };
    }

    private async IAsyncEnumerable<LookupRatingsOnTimestampResponse.ResponseRating> GenerateResponses(int[] userIds, int lastMatch, DateTime requestedTimestamp)
    {
        foreach (var userId in userIds)
        {
            var query = _context.RatingHistories
                // Filter by last lobby at requested timestamp
                .Where(x => x.MatchId <= lastMatch)
                // Filter by global rating
                .Where(x => x.RatingAttributeId == 0)
                // Filter by user
                .Where(x => x.PlayerId == userId)

                // Take last available rating for this time
                .OrderByDescending(x => x.MatchId)
                .ThenByDescending(x => x.GameId)
                .Select(x => new LookupRatingsOnTimestampResponse.ResponseRating
                {
                    UserId = x.PlayerId,
                    Username = _context.Players.First(z => z.PlayerId == x.PlayerId).ActiveUsername,
                    Rating = x.NewOrdinal,
                    LastUpdateTime = x.Match.EndTime
                });

            var queryResult = await query.FirstOrDefaultAsync();

            if (queryResult is not null)
            {
                yield return queryResult;
                continue;
            }

            yield return await GetEmptyUserRating(userId, requestedTimestamp);
        }
    }

    private async Task<LookupRatingsOnTimestampResponse.ResponseRating> GetEmptyUserRating(int userId, DateTime requestedTimestamp)
    {
        var player = await _playerService.GetPlayerById(userId);

        return new LookupRatingsOnTimestampResponse.ResponseRating
        {
            UserId = userId,
            Username = player?.ActiveUsername,
            Rating = 0,
            LastUpdateTime = requestedTimestamp
        };
    }
}
