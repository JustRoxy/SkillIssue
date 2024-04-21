using MediatR;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using SkillIssue.Domain.Unfair.Entities;
using TheGreatSpy.Services;

namespace SkillIssue.API.Commands;

public class GetHistoryRequest : IRequest<GetHistoryResponse>
{
    public List<string> Usernames { get; set; } = [];
    public DateTime Moment { get; set; } = DateTime.UtcNow;
}

public class GetHistoryResponse
{
    public List<RatingResponse> Ratings { get; set; } = [];
    public List<string> PlayersNotFound { get; set; } = [];

    public class RatingResponse
    {
        public int PlayerId { get; set; }
        public string ActiveUsername { get; set; }
        public int Value { get; set; }
    }
}

public class GetHistoryHandler(PlayerService playerService, DatabaseContext context)
    : IRequestHandler<GetHistoryRequest, GetHistoryResponse>
{
    public async Task<GetHistoryResponse> Handle(GetHistoryRequest request, CancellationToken cancellationToken)
    {
        List<Player> players = [];
        List<string> notFound = [];
        foreach (var username in request.Usernames)
        {
            var player = await playerService.GetPlayerByUsername(username);
            if (player is null) notFound.Add(username);
            else players.Add(player);
        }

        if (notFound.Any())
            return new GetHistoryResponse
            {
                PlayersNotFound = notFound
            };

        var playerIds = players.Select(x => x.PlayerId).ToList();

        var timestamp = request.Moment.ToUniversalTime();
        var history = await context.RatingHistories
            .Where(x => x.RatingAttributeId == 0 && playerIds.Contains(x.PlayerId))
            .Where(x => x.Match.EndTime < timestamp)
            .Select(x => new
            {
                x.PlayerId,
                x.MatchId,
                x.NewOrdinal
            })
            .GroupBy(x => x.PlayerId)
            .Select(x => new
            {
                x.Key,
                Ordinal = x.OrderByDescending(z => z.MatchId).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.Key, x => x.Ordinal, cancellationToken);
        var response = new GetHistoryResponse();
        foreach (var player in players)
        {
            var rating = history.GetValueOrDefault(player.PlayerId, null);
            var ordinal = rating?.NewOrdinal ?? 0;

            response.Ratings.Add(new GetHistoryResponse.RatingResponse
            {
                PlayerId = player.PlayerId,
                ActiveUsername = player.ActiveUsername,
                Value = ordinal
            });
        }

        return response;
    }
}