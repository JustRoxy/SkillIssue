using System.Text;
using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using SkillIssue.Discord.Commands.RatingCommands;
using SkillIssue.Domain.Discord;
using SkillIssue.Domain.Extensions;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;

namespace SkillIssue.Discord.Commands.LeaderboardCommands;

[Group("leaderboard", "Leaderboard commands")]
public class LeaderboardCommands(DatabaseContext context, ILogger<LeaderboardCommands> logger)
    : CommandBase<LeaderboardCommands>
{
    public enum LeaderboardAdditionalScorings
    {
        StarRating,
        Winrate
    }

    public enum LeaderboardRankRange
    {
        BelowThreeDigit,
        ThreeDigit,
        FourDigit,
        FiveDigit,
        SixDigit
    }

    private const int PageSize = 10;
    private static readonly Emoji TrackPrevious = new("\u23ee\ufe0f");
    private static readonly Emoji TrackNext = new("\u23ed\ufe0f");

    private static readonly Emoji LeftArrow = new("\u2b05\ufe0f");
    private static readonly Emoji RightArrow = new("\u27a1\ufe0f");

    protected override ILogger<LeaderboardCommands> Logger { get; } = logger;

    [ComponentInteraction("leaderboard.pager-*", true)]
    public async Task LeaderboardButtonComponent(string id)
    {
        await Catch(async () =>
        {
            var component = (SocketMessageComponent)Context.Interaction;
            var interaction = await context.Interactions.FindAsync(component.Message.Id);
            if (interaction is null || !await CheckUserId(interaction)) return;

            await DeferAsync();

            var state = LeaderboardState.Deserialize(interaction);

            var button = Enum.Parse<LeaderboardButtons>(id);

            switch (button)
            {
                case LeaderboardButtons.First:
                    state.Page = 0;
                    break;
                case LeaderboardButtons.Previous:
                    state.Page--;
                    break;
                case LeaderboardButtons.Next:
                    state.Page++;
                    break;
                case LeaderboardButtons.Last:
                    state.Page = state.LastPage;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var (embed, nextComponent) = await LeaderboardImpl(state);

            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed;
                x.Components = nextComponent;
            });

            interaction.StatePayload = state.Serialize();
            await context.SaveChangesAsync();
        });
    }

    [ComponentInteraction("leaderboard.attrib-*", true)]
    public async Task LeaderboardSelectionComponentComponent(string selectedType, string[] selected)
    {
        await Catch(async () =>
        {
            var component = (SocketMessageComponent)Context.Interaction;
            var interaction = await context.Interactions.FindAsync(component.Message.Id);
            if (interaction is null || !await CheckUserId(interaction)) return;

            await DeferAsync();

            var selectedValue = int.Parse(selected[0]);

            var state = LeaderboardState.Deserialize(interaction);
            var attributes = RatingAttribute.GetAttributesFromId(state.RatingAttributeId);

            switch (selectedType)
            {
                case "mod":
                {
                    attributes.modification = (ModificationRatingAttribute)selectedValue;
                    state.Page = 0;
                    break;
                }
                case "skill":
                {
                    attributes.skillset = (SkillsetRatingAttribute)selectedValue;
                    state.Page = 0;
                    break;
                }
                case "score":
                {
                    attributes.scoring = (ScoringRatingAttribute)selectedValue;

                    break;
                }
            }

            if (!RatingAttribute.UsableRatingAttribute(attributes.modification, attributes.skillset))
                attributes.skillset = SkillsetRatingAttribute.Overall;

            state.RatingAttributeId =
                RatingAttribute.GetAttributeId(attributes.modification, attributes.skillset, attributes.scoring);

            var (embed, newComponent) = await LeaderboardImpl(state);

            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed;
                x.Components = newComponent;
            });

            interaction.StatePayload = state.Serialize();
            await context.SaveChangesAsync();
        });
    }

    private async Task<(Embed embed, MessageComponent components)> LeaderboardImpl(LeaderboardState state,
        bool firstRun = false)
    {
        var ratingAttribute = RatingAttribute.GetAttribute(state.RatingAttributeId);

        if (state.BottomRankRange > state.TopRankRange)
        {
            (state.BottomRankRange, state.TopRankRange) = (state.TopRankRange, state.BottomRankRange);
        }

        #region Fetch data

        var leaderboardBaseQuery =
            context.Ratings
                .AsNoTracking()
                .Include(x => x.Player)
                .Include(x => x.RatingAttribute)
                .Where(x => x.RatingAttributeId == state.RatingAttributeId)
                .Ranked()
                .Case(state.BottomRankRange is not null, z => z.Where(x => x.Player.GlobalRank >= state.BottomRankRange))
                .Case(state.TopRankRange is not null, z => z.Where(x => x.Player.GlobalRank <= state.TopRankRange))
                .Case(!string.IsNullOrEmpty(state.CountryCode),
                    x => x.Where(z => z.Player.CountryCode == state.CountryCode))
                .Case(state.TopRankRange is null && state.BottomRankRange is null, z => z
                    .Case(state.RankRange == LeaderboardRankRange.BelowThreeDigit,
                        query => query.Where(x => x.Player.Digit == 1 || x.Player.Digit == 2))
                    .Case(state.RankRange == LeaderboardRankRange.ThreeDigit,
                        query => query.Where(x => x.Player.Digit == 3))
                    .Case(state.RankRange == LeaderboardRankRange.FourDigit,
                        query => query.Where(x => x.Player.Digit == 4))
                    .Case(state.RankRange == LeaderboardRankRange.FiveDigit,
                        query => query.Where(x => x.Player.Digit == 5))
                    .Case(state.RankRange == LeaderboardRankRange.SixDigit,
                        query => query.Where(x => x.Player.Digit == 6))
                )
                .If(state.AdditionalScorings == LeaderboardAdditionalScorings.StarRating,
                    x => x.OrderByDescending(z => z.StarRating),
                    x => x
                        .If(state.AdditionalScorings == LeaderboardAdditionalScorings.Winrate,
                            winrate => winrate.OrderByDescending(z => z.Winrate),
                            query => query.OrderByDescending(z => z.Ordinal)))
                .Where(x => x.TotalOpponentsAmount != 0)
                .Select(z => new
                {
                    z.RatingAttributeId,
                    z.PlayerId,
                    z.Ordinal,
                    z.GamesPlayed,
                    z.Player,
                    z.RatingAttribute,
                    z.StarRating,
                    IsInactive = context.Scores
                                     .OrderByDescending(y => y.MatchId)
                                     .First(y => y.PlayerId == z.PlayerId).Match.StartTime <
                                 DateTime.UtcNow - TimeSpan.FromDays(365),
                    Winrate = (double)z.WinAmount / z.TotalOpponentsAmount
                })
                .Case(state.HideInactive, x => x.Where(z => !z.IsInactive));

        var total = await leaderboardBaseQuery.CountAsync();

        if (total == 0)
            if (firstRun)
                throw new UserInteractionException(
                    $"Country code {state.CountryCode} is invalid (or no one plays there) D:");

        var totalPages = total / PageSize;
        if (total != 0 && total % PageSize == 0) totalPages--;

        state.LastPage = totalPages;
        var leaderboard = total == 0
            ? []
            : await leaderboardBaseQuery
                .Skip(state.Page * PageSize)
                .Take(PageSize)
                .ToListAsync();

        #endregion

        #region Embed

        string RankRange(LeaderboardRankRange? range)
        {
            return range switch
            {
                LeaderboardRankRange.BelowThreeDigit => "#1-#99",
                LeaderboardRankRange.ThreeDigit => "#100-#999",
                LeaderboardRankRange.FourDigit => "#1,000-#9,999",
                LeaderboardRankRange.FiveDigit => "#10,000-#99,999",
                LeaderboardRankRange.SixDigit => "#100,000-#999,999",
                null => "",
                _ => throw new ArgumentOutOfRangeException(nameof(range), range, null)
            };
        }

        string GenerateTitle()
        {
            var rankRange = state.RankRange == null
                ? ""
                : $"[{RankRange(state.RankRange)}] ";


            var scoring = state.AdditionalScorings switch
            {
                null => RatingAttribute.DescriptionFormat(ratingAttribute.Scoring),
                LeaderboardAdditionalScorings.StarRating => "Star Rating",
                LeaderboardAdditionalScorings.Winrate =>
                    $"{RatingAttribute.DescriptionFormat(ratingAttribute.Scoring)} winrate",
                _ => throw new ArgumentOutOfRangeException()
            };

            var modifications =
                $"{RatingAttribute.DescriptionFormat(ratingAttribute.Modification)} ({RatingAttribute.DescriptionFormat(ratingAttribute.Skillset)})";

            return $"{rankRange}{scoring} Leaderboard for {modifications}";
        }


        var embed = new EmbedBuilder().WithTitle(GenerateTitle())
            .WithThumbnailUrl(leaderboard.FirstOrDefault()?.Player.AvatarUrl)
            .WithFooter($"Page: {state.Page + 1}/{totalPages + 1}");

        var builder = new StringBuilder();

        var rank = state.Page * PageSize;

        foreach (var rating in leaderboard)
        {
            ++rank;
            var ordinal = state.AdditionalScorings == LeaderboardAdditionalScorings.StarRating
                ? $"{rating.StarRating:F3}\\*"
                : state.AdditionalScorings == LeaderboardAdditionalScorings.Winrate
                    ? $"{rating.Winrate:P2}"
                    : ratingAttribute.Scoring == ScoringRatingAttribute.PP
                        ? $"{rating.Ordinal:N0} PP"
                        : $"{rating.Ordinal:N0} SIP";

            var starRatingText = state.AdditionalScorings == LeaderboardAdditionalScorings.StarRating
                ? ""
                : $"[{rating.StarRating:N2}\\*]";

            var contentString =
                $"{Format.Url(rating.Player.ActiveUsername, rating.Player.GetUrl())} - {ordinal} {starRatingText}";

            if (rating.IsInactive) contentString = Format.Strikethrough(contentString);
            builder.AppendLine(
                $"{rank}: {contentString}");
        }

        embed.AddField(
            string.IsNullOrWhiteSpace(state.CountryCode)
                ? "Global"
                : $"Country :flag_{state.CountryCode.ToLower()}:",
            builder.Length == 0 ? "No one :sob:" : builder.ToString());

        #endregion

        #region SelectMenu

        var modificationMenu = new SelectMenuBuilder()
            .WithMinValues(1).WithMaxValues(1)
            .WithPlaceholder("Modification")
            .WithCustomId("leaderboard.attrib-mod");

        foreach (var mod in Enum.GetValues<ModificationRatingAttribute>())
            modificationMenu.AddOption($"{RatingAttribute.DescriptionFormat(mod)}", $"{(int)mod}",
                isDefault: mod == ratingAttribute.Modification);

        var skillsetMenu = new SelectMenuBuilder()
            .WithMinValues(1).WithMaxValues(1)
            .WithPlaceholder("Skillset")
            .WithCustomId("leaderboard.attrib-skill");

        foreach (var skillset in Enum.GetValues<SkillsetRatingAttribute>())
            if (RatingAttribute.UsableRatingAttribute(ratingAttribute.Modification, skillset))
            {
                var label = RatingAttribute.DescriptionFormat(skillset);
                skillsetMenu.AddOption(label, $"{(int)skillset}",
                    isDefault: skillset == ratingAttribute.Skillset);
            }

        var scoringMenu = new SelectMenuBuilder()
            .WithMinValues(1).WithMaxValues(1)
            .WithPlaceholder("Scoring")
            .WithCustomId("leaderboard.attrib-score");

        foreach (var scoring in Enum.GetValues<ScoringRatingAttribute>())
            scoringMenu.AddOption($"{RatingAttribute.DescriptionFormat(scoring)}",
                $"{(int)scoring}", isDefault: scoring == ratingAttribute.Scoring);

        #endregion

        #region Buttons

        var firstButton = new ButtonBuilder()
            .WithCustomId($"leaderboard.pager-{LeaderboardButtons.First}")
            .WithDisabled(state.Page == 0)
            .WithEmote(TrackPrevious)
            .WithStyle(ButtonStyle.Primary);

        var previousPageButton = new ButtonBuilder()
            .WithCustomId($"leaderboard.pager-{LeaderboardButtons.Previous}")
            .WithDisabled(state.Page == 0)
            .WithEmote(LeftArrow)
            .WithStyle(ButtonStyle.Primary);

        var nextPageButton = new ButtonBuilder()
            .WithCustomId($"leaderboard.pager-{LeaderboardButtons.Next}")
            .WithDisabled(state.Page == totalPages)
            .WithEmote(RightArrow)
            .WithStyle(ButtonStyle.Primary);

        var lastButton = new ButtonBuilder()
            .WithCustomId($"leaderboard.pager-{LeaderboardButtons.Last}")
            .WithDisabled(state.Page == totalPages)
            .WithEmote(TrackNext)
            .WithStyle(ButtonStyle.Primary);

        #endregion

        var menuBuilder = new ComponentBuilder()
            .WithButton(firstButton)
            .WithButton(previousPageButton)
            .WithButton(nextPageButton)
            .WithButton(lastButton)
            .WithSelectMenu(modificationMenu)
            .WithSelectMenu(skillsetMenu);

        if (state.AdditionalScorings != LeaderboardAdditionalScorings.StarRating)
            menuBuilder.WithSelectMenu(scoringMenu);
        return (embed.Build(), menuBuilder.Build());
    }

    private void ValidateBottomTopRankRange(int? bottomRankRange, int? topRankRange)
    {
        if (bottomRankRange is < 1 or > 999_999) throw new UserInteractionException("bottom-rank-range must be from 1 to 999999");
        if (topRankRange is < 1 or > 999_999) throw new UserInteractionException("top-rank-range must be from 1 to 999999");
    }

    [SlashCommand("global", "Get global leaderboard")]
    public async Task Leaderboard(
        [Summary(description: "Filters leaderboard by the chosen rank-range")]
        LeaderboardRankRange? rankRange = null,
        [Summary(description: "Bottom rank range, overrides rank-range")]
        int? bottomRankRange = null,
        [Summary(description: "Top rank range, overrides rank-range")]
        int? topRankRange = null,
        [Summary(description: "Provides info on players' other aspects")]
        LeaderboardAdditionalScorings? additionalScorings = null,
        [Summary(description: "Hides inactive players")]
        bool hideInactive = false)
    {
        await Catch(async () =>
        {
            await DeferAsync();
            ValidateBottomTopRankRange(bottomRankRange, topRankRange);

            var state = new LeaderboardState
            {
                RatingAttributeId = 0,
                CountryCode = null,
                Page = 0,
                RankRange = rankRange,
                AdditionalScorings = additionalScorings,
                HideInactive = hideInactive,
                BottomRankRange = bottomRankRange,
                TopRankRange = topRankRange,
            };

            var interaction = new InteractionState
            {
                CreatorId = Context.User.Id,
                PlayerId = null,
                CreationTime = DateTime.UtcNow
            };

            var (embed, component) = await LeaderboardImpl(state, true);
            var message = await FollowupAsync(embed: embed, components: component);

            interaction.MessageId = message.Id;
            interaction.StatePayload = state.Serialize();
            context.Interactions.Add(interaction);
            await context.SaveChangesAsync();
        });
    }

    [SlashCommand("country", "Get country leaderboard")]
    public async Task LeaderboardCountry(
        [Summary(description: "Two characters country code")] [MinLength(2)] [MaxLength(2)]
        string country,
        [Summary(description: "Filters country leaderboard by the chosen rank-range")]
        LeaderboardRankRange? rankRange = null,
        [Summary(description: "Bottom rank range, overrides rank-range")]
        int? bottomRankRange = null,
        [Summary(description: "Top rank range, overrides rank-range")]
        int? topRankRange = null,
        [Summary(description: "Provides info on players' other aspects")]
        LeaderboardAdditionalScorings? additionalScorings = null,
        [Summary(description: "Hides inactive players")]
        bool hideInactive = false)
    {
        await Catch(async () =>
        {
            await DeferAsync();
            ValidateBottomTopRankRange(bottomRankRange, topRankRange);

            var state = new LeaderboardState
            {
                RatingAttributeId = 0,
                CountryCode = country.ToUpper(),
                Page = 0,
                RankRange = rankRange,
                AdditionalScorings = additionalScorings,
                HideInactive = hideInactive,
                BottomRankRange = bottomRankRange,
                TopRankRange = topRankRange,
            };

            var interaction = new InteractionState
            {
                CreatorId = Context.User.Id,
                PlayerId = null,
                CreationTime = DateTime.UtcNow
            };

            var (embed, component) = await LeaderboardImpl(state, true);
            var message = await FollowupAsync(embed: embed, components: component);

            interaction.MessageId = message.Id;
            interaction.StatePayload = state.Serialize();
            context.Interactions.Add(interaction);
            await context.SaveChangesAsync();
        });
    }

    private enum LeaderboardButtons
    {
        First,
        Previous,
        Next,
        Last
    }

    private class LeaderboardState
    {
        public int RatingAttributeId { get; set; }
        public string? CountryCode { get; set; }
        public int Page { get; set; }
        public int LastPage { get; set; }
        public bool HideInactive { get; set; }

        public LeaderboardAdditionalScorings? AdditionalScorings { get; set; }
        public LeaderboardRankRange? RankRange { get; set; }
        public int? BottomRankRange { get; set; } = null;
        public int? TopRankRange { get; set; } = null;

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static LeaderboardState Deserialize(InteractionState state)
        {
            return JsonSerializer.Deserialize<LeaderboardState>(state.StatePayload)!;
        }
    }
}