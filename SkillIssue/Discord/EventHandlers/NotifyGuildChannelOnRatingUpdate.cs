using Discord;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SkillIssue.Database;
using SkillIssue.Discord.Extensions;
using SkillIssue.Domain.Events.Matches;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;

namespace SkillIssue.Discord.EventHandlers;

public class NotifyGuildChannelOnRatingUpdate(
    IOptions<DiscordConfig> config,
    DatabaseContext context,
    IDiscordClient discordClient)
    : INotificationHandler<MatchCalculated>
{
    public async Task Handle(MatchCalculated notification, CancellationToken cancellationToken)
    {
        if (notification.RatingChanges.Count == 0) return;

        var channel = (ITextChannel)await discordClient.GetChannelAsync(config.Value.UpdatesChannel);

        var updates = notification.RatingChanges
            .OrderBy(x => x.GameId)
            .GroupBy(x => (x.PlayerId, x.RatingAttributeId))
            .Select(x => new RatingHistory
            {
                // GameId = 0,
                PlayerId = x.Key.PlayerId,
                RatingAttributeId = x.Key.RatingAttributeId,
                MatchId = notification.Match.MatchId,
                NewStarRating = x.Last().NewStarRating,
                OldStarRating = x.First().OldStarRating,
                NewOrdinal = x.Last().NewOrdinal,
                OldOrdinal = x.First().OldOrdinal
            });

        var ratingUpdates = updates.GroupBy(x => x.PlayerId).ToList();

        var playerIds = ratingUpdates.Select(x => x.Key).ToList();
        var players = await context.Players
            .AsNoTracking()
            .Where(x => playerIds.Contains(x.PlayerId))
            .Select(x => new Player
            {
                PlayerId = x.PlayerId,
                ActiveUsername = x.ActiveUsername,
                CountryCode = x.CountryCode,
                AvatarUrl = x.AvatarUrl
            }).ToDictionaryAsync(x => x.PlayerId, cancellationToken);

        var embeds = new List<Embed>();
        foreach (var ratingUpdate in ratingUpdates)
        {
            var player = players[ratingUpdate.Key];

            var globalRating = ratingUpdate.First(x => x.RatingAttributeId == 0);
            var playerEmbed = new EmbedBuilder()
                .WithTitle(player.ActiveUsername)
                .WithUrl($"https://osu.ppy.sh/community/matches/{notification.Match.MatchId}")
                .WithThumbnailUrl(player.AvatarUrl)
                .WithFooter(notification.Match.Name)
                .WithTimestamp(notification.Match.StartTime.ToUniversalTime())
                .WithColor(globalRating.OrdinalDelta == 0 ? Color.LightGrey :
                    globalRating.OrdinalDelta > 0 ? Color.Green : Color.Red);
            AddProfileMods(playerEmbed, ratingUpdate,
                notification.PlayerHistories.First(x => x.PlayerId == ratingUpdate.Key));
            embeds.Add(playerEmbed.Build());
        }

        foreach (var embed in embeds) await channel.SendMessageAsync(embed: embed);
    }

    private static void AddProfileMods(EmbedBuilder embedBuilder, IGrouping<int, RatingHistory> ratings,
        PlayerHistory playerHistory)
    {
        var overall = ratings.First(x => x.RatingAttributeId == 0);
        var pp = ratings.FirstOrDefault(x => x.RatingAttributeId == (int)ScoringRatingAttribute.PP);
        foreach (var rating in ratings) rating.RatingAttribute = RatingAttribute.GetAttribute(rating.RatingAttributeId);

        var mods = ratings
            .Where(x => x.RatingAttributeId != 0 && x.RatingAttribute is
                { IsMajor: true, Scoring: ScoringRatingAttribute.Score })
            .OrderBy(x => x.RatingAttributeId)
            .ToList();

        if (pp?.OrdinalDelta >= 1)
            embedBuilder.AddField("PP", $"{pp.OldOrdinal} -> {pp.NewOrdinal} ({pp.OrdinalDelta})");

        if (Math.Abs(overall.StarRatingDelta) >= 0.01f)
            embedBuilder.AddField("SR",
                $"{overall.OldStarRating:F3} -> {overall.NewStarRating:F3} ({overall.StarRatingDelta:F2})");

        embedBuilder.WithDescription(
            $"SIP: {overall.OldOrdinal:N0} -> {overall.NewOrdinal:N0} ({overall.OrdinalDelta:N0})\nMatch Cost: {playerHistory.MatchCost:F3}");
        foreach (var mod in mods.Where(x => x.RatingAttribute.Skillset == SkillsetRatingAttribute.Overall))
        {
            var modificationValue = RatingAttribute.GetAttributesFromId(mod.RatingAttributeId).modification;
            embedBuilder.AddField($"{modificationValue.ToEmote()}{ShortModName(modificationValue)}",
                $"{mod.OldOrdinal:N0} -> {mod.NewOrdinal:N0} ({mod.OrdinalDelta:N0})");
        }

        foreach (var mod in mods.Where(x => x.RatingAttribute.Skillset != SkillsetRatingAttribute.Overall))
        {
            var skillset = RatingAttribute.GetAttributesFromId(mod.RatingAttributeId).skillset;
            embedBuilder.AddField($"{skillset.ToEmote()}{RatingAttribute.DescriptionFormat(skillset)}",
                $"{mod.OldOrdinal:N0} -> {mod.NewOrdinal:N0} ({mod.OrdinalDelta:N0})");
        }
    }

    private static string ShortModName(ModificationRatingAttribute modificationRatingAttribute)
    {
        return modificationRatingAttribute switch
        {
            ModificationRatingAttribute.NM => "NM",
            ModificationRatingAttribute.HD => "HD",
            ModificationRatingAttribute.HR => "HR",
            ModificationRatingAttribute.DT => "DT",
            ModificationRatingAttribute.AllMods => "SIP",
            _ => throw new ArgumentOutOfRangeException(nameof(modificationRatingAttribute), modificationRatingAttribute,
                null)
        };
    }
}