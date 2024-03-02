using System.Text.Json;
using MediatR;
using SkillIssue.Domain.Events.Matches;
using SkillIssue.Domain.Unfair.Entities;
using TheGreatSpy.Services;

namespace TheGreatSpy.Handlers;

public class UpdatePlayerInfoHandler(PlayerService playerService)
    : INotificationHandler<MatchUpdated>
{
    public async Task Handle(MatchUpdated notification, CancellationToken cancellationToken)
    {
        var match = notification.DeserializedMatch;

        var playerList = match!["users"]!.AsArray()
            .Select(x => new Player
            {
                PlayerId = x!["id"].Deserialize<int>(),
                ActiveUsername = x["username"].Deserialize<string>()!,
                CountryCode = x["country_code"].Deserialize<string>()!,
                AvatarUrl = x["avatar_url"].Deserialize<string>()!,
                IsRestricted = false
            }).ToList();

        await playerService.UpsertPlayers(playerList);
    }
}