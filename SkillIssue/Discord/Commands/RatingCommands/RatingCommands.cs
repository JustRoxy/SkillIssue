using System.Text;
using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using osu.Game.Beatmaps.Legacy;
using SkillIssue.Database;
using SkillIssue.Discord.Extensions;
using SkillIssue.Domain.Discord;
using SkillIssue.Domain.Extensions;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;
using TheGreatSpy.Services;

namespace SkillIssue.Discord.Commands.RatingCommands;

public class UserInteractionException(string message) : Exception(message);

internal static class RatingCommandExtensions
{
    public static EmbedBuilder RankColor(this EmbedBuilder embedBuilder, Rating rating, int rank)
    {
        var color = rank switch
        {
            < 100 => Color.Gold,
            < 500 => Color.Purple,
            < 1000 => Color.Blue,
            < 5000 => Color.LightGrey,
            _ => Color.LighterGrey
        };

        var status = rating.GetCurrentStatus();

        if (status == RatingStatus.Calibration) color = Color.DarkGrey;

        return embedBuilder.WithColor(color);
    }
}

public class RatingCommands(
    DatabaseContext context,
    PlayerService playerService,
    ILogger<RatingCommands> logger)
    : CommandBase<RatingCommands>
{
    private static readonly Emoji AdultEmoji = Emoji.Parse(":adult:");
    private static readonly Emoji BarChartEmoji = Emoji.Parse(":bar_chart:");
    protected override ILogger<RatingCommands> Logger { get; } = logger;

    private async Task<(int globalRank, int countryRank)> GetRanks(string countryCode, Rating rating)
    {
        var globalRank = await context.Ratings
            .Ranked()
            .CountAsync(x => x.RatingAttributeId == rating.RatingAttributeId
                             && x.Ordinal > rating.Ordinal) + 1;

        var countryRank = await context.Ratings
            .Ranked()
            .CountAsync(x => x.Player!.CountryCode == countryCode
                             && x.RatingAttributeId == rating.RatingAttributeId
                             && x.Ordinal > rating.Ordinal) + 1;

        return (globalRank, countryRank);
    }

    private static string GetFancyLine(double left, int total = 10)
    {
        var sb = new StringBuilder("[");

        var points = (int)Math.Round(left * total, MidpointRounding.AwayFromZero);
        string borderSymbol;
        var middle = total / 2;

        if (points == middle) borderSymbol = "|";
        else if (points < middle) borderSymbol = "<";
        else borderSymbol = ">";

        for (var i = 0; i < total; i++)
        {
            sb.Append('-');

            // if (i > points)
            // {
            //     sb.Append("\u2800");
            // }

            if (i + 1 == points) sb.Append(borderSymbol);
        }

        sb.Append(']');

        return sb.ToString();
    }

    private MessageComponent GenerateButtons(RatingEmbedState state)
    {
        var componentBuilder = new ComponentBuilder();
        var profileButton = new ButtonBuilder()
            .WithDisabled(state.State == RatingEmbedStates.Profile)
            .WithEmote(AdultEmoji)
            .WithStyle(ButtonStyle.Secondary)
            .WithLabel("profile")
            .WithCustomId($"rating.menus-{RatingEmbedButtons.ToProfile}");

        var statsButton = new ButtonBuilder()
            .WithDisabled(state.State == RatingEmbedStates.Statistics)
            .WithEmote(BarChartEmoji)
            .WithStyle(ButtonStyle.Secondary)
            .WithLabel("stats")
            .WithCustomId($"rating.menus-{RatingEmbedButtons.ToStats}");

        componentBuilder.WithButton(profileButton).WithButton(statsButton);
        if (state.State == RatingEmbedStates.Profile)
        {
            var ppButton = new ButtonBuilder()
                .WithStyle(ButtonStyle.Secondary)
                .WithCustomId($"rating.menus-{RatingEmbedButtons.FlipPp}");

            if (state.IsPpScoring)
                ppButton.WithLabel("SIP").WithEmote(EmotesExtensions.RankedEmoji);
            else
                ppButton.WithLabel("PP").WithEmote(EmotesExtensions.OverallEmoji);

            componentBuilder.WithButton(ppButton);
        }

        if (state.State == RatingEmbedStates.Profile) return componentBuilder.Build();

        var modificationMenu = new SelectMenuBuilder()
            .WithMinValues(1)
            .WithMaxValues(1)
            .WithPlaceholder("Modification")
            .WithCustomId("rating.mod");

        foreach (var mod in Enum.GetValues<ModificationRatingAttribute>())
            modificationMenu.AddOption($"{RatingAttribute.DescriptionFormat(mod)}", $"{(int)mod}",
                isDefault: mod == state.SelectedMod);

        componentBuilder.WithSelectMenu(modificationMenu);

        return componentBuilder.Build();
    }

    private Task<(Embed embed, string? message)> GenerateEmbed(Player player, RatingEmbedState state)
    {
        return state.State == RatingEmbedStates.Profile
            ? GenerateProfileEmbed(player, state)
            : GenerateStatsEmbed(player, state);
    }

    private async Task<(Embed, string?)> GenerateStatsEmbed(Player player, RatingEmbedState state)
    {
        var ratingsList = await context.Ratings
            .AsNoTracking()
            .Include(x => x.RatingAttribute)
            .Where(x => x.PlayerId == player.PlayerId && x.RatingAttribute.Modification == state.SelectedMod)
            .OrderBy(x => x.RatingAttributeId)
            .Select(x => new
            {
                x.RatingAttributeId,
                x.PlayerId,
                x.Mu,
                x.Sigma,
                x.Ordinal,
                x.GamesPlayed,
                x.RatingAttribute,
                x.StarRating,
                x.Status,
                GlobalRank =
                    context.Ratings.Ranked()
                        .Count(z => z.RatingAttributeId == x.RatingAttributeId && z.Ordinal > x.Ordinal) + 1,
                CountryRank = context.Ratings.Ranked().Count(z => z.Player!.CountryCode == player.CountryCode
                                                                  && z.RatingAttributeId == x.RatingAttributeId &&
                                                                  z.Ordinal > x.Ordinal) + 1
            })
            .ToListAsync();

        var ratings = ratingsList.GroupBy(x => x.RatingAttribute.Skillset).ToList();
        var embed = new EmbedBuilder()
            .WithTitle($":flag_{player.CountryCode.ToLower()}: {player.ActiveUsername}")
            .WithUrl(player.GetUrl())
            .WithThumbnailUrl(player.AvatarUrl)
            .WithDescription(Format.Bold($"Statistics for {RatingAttribute.DescriptionFormat(state.SelectedMod)}"));

        foreach (var rating in ratings)
        {
            var score = rating.First(x => x.RatingAttribute.Scoring == ScoringRatingAttribute.Score);
            var accuracy = rating.First(x => x.RatingAttribute.Scoring == ScoringRatingAttribute.Accuracy);
            var combo = rating.First(x => x.RatingAttribute.Scoring == ScoringRatingAttribute.Combo);
            var pp = rating.FirstOrDefault(x => x.RatingAttribute.Scoring == ScoringRatingAttribute.PP);

            var sb = new StringBuilder();

            sb.AppendLine(
                $"Score: {Format.Bold(score.Ordinal.ToString("N0"))} (\ud83c\udf10 #{score.GlobalRank:N0}, :flag_{player.CountryCode.ToLower()}: #{score.CountryRank:N0})");
            sb.AppendLine(
                $"Combo: {Format.Bold(combo.Ordinal.ToString("N0"))} (\ud83c\udf10 #{combo.GlobalRank:N0}, :flag_{player.CountryCode.ToLower()}: #{combo.CountryRank:N0})");
            sb.AppendLine(
                $"Accuracy: {Format.Bold(accuracy.Ordinal.ToString("N0"))} (\ud83c\udf10 #{accuracy.GlobalRank:N0}, :flag_{player.CountryCode.ToLower()}: #{accuracy.CountryRank:N0})");
            if (pp is not null)
                sb.AppendLine(
                    $"PP: {Format.Bold(pp.Ordinal.ToString("N0"))} (\ud83c\udf10 #{pp.GlobalRank:N0}, :flag_{player.CountryCode.ToLower()}: #{pp.CountryRank:N0})");

            var status = score.Status == RatingStatus.Calibration ? "[In calibration]" : "";
            embed.AddField(
                $"{RatingAttribute.DescriptionFormat(rating.Key)} {score.StarRating:F3}* {status}",
                sb.ToString());
        }

        return (embed.Build(), null);
    }

    private async Task<(Embed, string?)> GenerateProfileEmbed(Player player, RatingEmbedState state)
    {
        var ratings = await context.Ratings
            .AsNoTrackingWithIdentityResolution()
            .Where(x => x.PlayerId == player.PlayerId)
            .Where(x => RatingAttribute.MajorAttributes.Contains(x.RatingAttributeId))
            .If(state.IsPpScoring,
                query => query.Where(x => x.RatingAttribute.Scoring == ScoringRatingAttribute.PP),
                query => query.Where(x => x.RatingAttribute.Scoring == ScoringRatingAttribute.Score))
            .OrderByDescending(x => x.Ordinal)
            .Select(x => new Rating
                {
                    RatingAttributeId = x.RatingAttributeId,
                    Ordinal = x.Ordinal,
                    StarRating = x.StarRating,
                    GamesPlayed = x.GamesPlayed,
                    RatingAttribute = x.RatingAttribute
                }
            )
            .ToListAsync();

        ratings = ratings.Where(x => x.RatingAttribute.IsMajor).ToList();
        var embed = new EmbedBuilder()
            .WithTitle($":flag_{player.CountryCode.ToLower()}: {player.ActiveUsername}")
            .WithUrl(player.GetUrl())
            .WithThumbnailUrl(player.AvatarUrl);

        if (ratings.Count == 0)
        {
            embed.AddField("Player has no ratings", ":smirk_cat:", true);
            embed.WithFooter("Ratings will be here after your first tournament match");
            return (embed.Build(), null);
        }

        var globalRating = ratings.First(x =>
            x.RatingAttribute is
                { Modification: ModificationRatingAttribute.AllMods, Skillset: SkillsetRatingAttribute.Overall });

        var (globalRank, countryRank) = await GetRanks(player.CountryCode, globalRating);

        embed.WithDescription(state.IsPpScoring
            ? Format.Bold($"PP: {globalRating.Ordinal:N0}")
            : Format.Bold($"SIP: {globalRating.Ordinal:N0}"));

        embed.RankColor(globalRating, globalRank)
            .AddField("Global rank", globalRank.ToString("N0"), true)
            .AddField("Country rank", countryRank.ToString("N0"), true)
            .WithFooter($"Star Rating: {globalRating.StarRating:F3}");

        foreach (var rating in ratings.Where(x => x != globalRating))
        {
            var title = rating.RatingAttribute.Skillset != SkillsetRatingAttribute.Overall
                ? $"{rating.RatingAttribute.Skillset.ToEmote()} {RatingAttribute.DescriptionFormat(rating.RatingAttribute.Skillset)}"
                : $"{rating.RatingAttribute.Modification.ToEmote()} {RatingAttribute.DescriptionFormat(rating.RatingAttribute.Modification)}";

            embed.AddField($"{title}", $"{rating.Ordinal:N0}");
        }

        return (embed.Build(), GetUnrankedMessage(globalRating));
    }

    private string? GetUnrankedMessage(Rating rating)
    {
        if (rating.Status == RatingStatus.Calibration)
            return "This player is still in calibration, provided embed can be incorrect";
        return null;
    }

    [ComponentInteraction("rating.mod")]
    public async Task RatingSelectMenuSelected(string _, string[] selected)
    {
        await Catch(async () =>
        {
            var component = (SocketMessageComponent)Context.Interaction;
            var interaction = await context.Interactions.FindAsync(component.Message.Id);
            if (interaction is null) throw new UserInteractionException("your interaction got deleted sowwy");

            if (!await CheckUserId(interaction)) return;

            await DeferAsync();

            var state = RatingEmbedState.Deserialize(interaction).ResetOnVersionMismatch();
            var nextMod = (ModificationRatingAttribute)int.Parse(selected[0]);

            state.SelectedMod = nextMod;
            interaction.StatePayload = state.Serialize();

            var player = await context.Players.FindAsync(interaction.PlayerId);

            var embed = await GenerateEmbed(player!, state);
            var buttons = GenerateButtons(state);
            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed.embed;
                x.Components = buttons;
                x.Content = embed.message;
            });

            await context.SaveChangesAsync();
        });
    }


    [ComponentInteraction("rating.menus-*")]
    public async Task RatingButtonPressed(string customId)
    {
        await Catch(async () =>
        {
            var component = (SocketMessageComponent)Context.Interaction;
            var interaction = await context.Interactions.FindAsync(component.Message.Id);
            if (interaction is null) throw new UserInteractionException("your interaction got deleted sowwy");

            if (!await CheckUserId(interaction)) return;

            var state = RatingEmbedState.Deserialize(interaction).ResetOnVersionMismatch();

            var action = Enum.Parse<RatingEmbedButtons>(customId);
            switch (action)
            {
                case RatingEmbedButtons.ToProfile:
                    state.State = RatingEmbedStates.Profile;
                    break;
                case RatingEmbedButtons.ToStats:
                    state.State = RatingEmbedStates.Statistics;
                    break;
                case RatingEmbedButtons.FlipPp:
                    state.IsPpScoring = !state.IsPpScoring;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await DeferAsync();

            var player = await context.Players.FindAsync(interaction.PlayerId);
            var embed = await GenerateEmbed(player!, state);
            var buttons = GenerateButtons(state);
            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed.embed;
                x.Components = buttons;
                x.Content = embed.message;
            });

            interaction.StatePayload = state.Serialize();
            await context.SaveChangesAsync();
        });
    }

    [SlashCommand("rating", "Get player rating", true)]
    public async Task Rating(string username)
    {
        await Catch(async () =>
        {
            await DeferAsync();

            var player = await HandlePlayerRequest(username, playerService);
            if (player is null) return;

            var state = new RatingEmbedState
            {
                IsPpScoring = false,
                State = RatingEmbedStates.Profile,
                SelectedMod = ModificationRatingAttribute.AllMods
            };

            var interaction = new InteractionState
            {
                CreatorId = Context.User.Id,
                PlayerId = player.PlayerId,
                CreationTime = DateTime.UtcNow
            };

            var embed = await GenerateEmbed(player, state);
            var message = await FollowupAsync(embed.message,
                embed: embed.embed,
                components: GenerateButtons(state));
            interaction.MessageId = message.Id;
            interaction.StatePayload = state.Serialize();

            context.Interactions.Add(interaction);
            await context.SaveChangesAsync();
        });
    }

    private async Task<(Stream file, string name)> HistoryImpl(InteractionState interactionState)
    {
        ArgumentNullException.ThrowIfNull(interactionState.PlayerId);
        var historyState = HistoryState.Deserialize(interactionState);

        var attribute = RatingAttribute.GetAttribute(historyState.RatingAttributeId);

        var activeUsername = await context.Players
            .AsNoTracking()
            .Where(x => x.PlayerId == interactionState.PlayerId)
            .Select(x => x.ActiveUsername)
            .FirstAsync();

        var history = await context.RatingHistories
            .AsNoTracking()
            .Where(x => x.PlayerId == interactionState.PlayerId && x.RatingAttributeId == attribute.AttributeId)
            .Include(x => x.Match)
            .Include(x => x.PlayerHistory)
            .Include(x => x.Score)
            .ThenInclude(x => x.Beatmap)
            .Case(!historyState.IncludeGameHistory, x => x
                .GroupBy(z => z.MatchId)
                .Select(z => new RatingHistory
                {
                    MatchId = z.OrderBy(y => y.GameId).First().MatchId,
                    NewStarRating = z.OrderBy(y => y.GameId).Last().NewStarRating,
                    NewOrdinal = z.OrderBy(y => y.GameId).Last().NewOrdinal,
                    Match = z.OrderBy(y => y.GameId).First().Match,
                    PlayerHistory = z.OrderBy(y => y.GameId).First().PlayerHistory
                }))
            .OrderBy(x => x.MatchId)
            .ToListAsync();

        var sb = new StringBuilder($"Rating history for {activeUsername} on {attribute.Description}\n");

        var ordinal = 0d;
        foreach (var rating in history.GroupBy(x => x.MatchId))
        {
            var match = rating.First().Match;
            var newOrdinal = rating.Last().NewOrdinal;
            var newStarRating = rating.Last().NewStarRating;
            var matchCost = rating.Last().PlayerHistory.MatchCost;
            sb.AppendLine(
                $"{match.StartTime.ToShortDateString()} | {match.Name}: {newOrdinal:N0} ({newOrdinal - ordinal:N0}) {matchCost:F2} {newStarRating:F2}* https://osu.ppy.sh/mp/{match.MatchId}");
            if (historyState.IncludeGameHistory)
                foreach (var r in rating)
                {
                    var beatmapName = r.Score.Beatmap is null
                        ? "Unknown beatmap"
                        : $"{r.Score.Beatmap.Artist} - {r.Score.Beatmap.Name}";

                    var mods = r.Score.LegacyMods & ~LegacyMods.NoFail;
                    var modsString = mods == LegacyMods.None ? "" : $" [{mods.ToString()}]";
                    sb.AppendLine(
                        $"\t{beatmapName}{modsString}: {r.OldOrdinal} -> {r.NewOrdinal}");
                }

            ordinal = newOrdinal;
        }

        var file = sb.ToString();
        return (new MemoryStream(Encoding.UTF8.GetBytes(file)),
            $"history-{activeUsername}-{RatingAttribute.GetCsvHeaderValue(attribute)}.txt");
    }

    [ComponentInteraction("rhs-*")]
    public async Task HandleRatingHistorySelection(string type, string[] selected)
    {
        await Catch(async () =>
        {
            var component = (SocketMessageComponent)Context.Interaction;
            var interactionState = (await context.Interactions.FindAsync(component.Message.Id))!;
            if (!await CheckUserId(interactionState)) return;

            await DeferAsync();

            var historyState = HistoryState.Deserialize(interactionState);

            var attributes = RatingAttribute.GetAttributesFromId(historyState.RatingAttributeId);
            var selectedValue = int.Parse(selected[0]);

            switch (type)
            {
                case "mod":
                {
                    attributes.modification = (ModificationRatingAttribute)selectedValue;
                    break;
                }
                case "skill":
                {
                    attributes.skillset = (SkillsetRatingAttribute)selectedValue;
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

            historyState.RatingAttributeId =
                RatingAttribute.GetAttributeId(attributes.modification, attributes.skillset, attributes.scoring);
            interactionState.StatePayload = historyState.Serialize();

            var (file, filename) = await HistoryImpl(interactionState);
            var selectionBuilder = new ComponentBuilder();

            foreach (var selectMenu in
                     GenerateAttributeSelectMenus(new RatingAttribute
                     {
                         Modification = attributes.modification,
                         Skillset = attributes.skillset,
                         Scoring = attributes.scoring,
                         AttributeId = historyState.RatingAttributeId
                     }, "rhs"))
                selectionBuilder.WithSelectMenu(selectMenu);


            await ModifyOriginalResponseAsync(x =>
            {
                x.Attachments = new Optional<IEnumerable<FileAttachment>>([new FileAttachment(file, filename)]);
                x.Components = selectionBuilder.Build();
            });

            await context.SaveChangesAsync();
        });
    }

    [SlashCommand("history", "Get player's history", true)]
    public async Task History(string username,
        [Summary(description: "Include game history")]
        bool includeGameHistory = false)
    {
        await Catch(async () =>
        {
            await DeferAsync();
            var player = await HandlePlayerRequest(username, playerService);
            if (player is null) return;

            var historyState = new HistoryState
            {
                RatingAttributeId = 0,
                IncludeGameHistory = includeGameHistory
            };

            var interactionState = new InteractionState
            {
                CreatorId = Context.User.Id,
                MessageId = 0,
                PlayerId = player.PlayerId,
                CreationTime = DateTime.UtcNow,
                StatePayload = historyState.Serialize()
            };
            var (file, filename) = await HistoryImpl(interactionState);

            var selectionBuilder = new ComponentBuilder();

            foreach (var selectMenu in GenerateAttributeSelectMenus(RatingAttribute.GetAttribute(0), "rhs"))
                selectionBuilder.WithSelectMenu(selectMenu);

            var message = await FollowupWithFileAsync(file, filename, components: selectionBuilder.Build());
            interactionState.MessageId = message.Id;

            context.Interactions.Add(interactionState);
            await context.SaveChangesAsync();
        });
    }

    private enum RatingEmbedButtons
    {
        ToProfile,
        ToStats,
        FlipPp
    }

    private enum RatingEmbedStates
    {
        Profile,
        Statistics
    }

    private class RatingEmbedState
    {
        private const int CurrentVersion = 1;
        public int Version { get; set; } = CurrentVersion;
        public required bool IsPpScoring { get; set; }
        public required RatingEmbedStates State { get; set; }
        public required ModificationRatingAttribute SelectedMod { get; set; }

        public RatingEmbedState ResetOnVersionMismatch()
        {
            if (Version != CurrentVersion)
            {
                IsPpScoring = false;
                State = RatingEmbedStates.Profile;
                SelectedMod = ModificationRatingAttribute.AllMods;
            }

            return this;
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static RatingEmbedState Deserialize(InteractionState state)
        {
            return JsonSerializer.Deserialize<RatingEmbedState>(state.StatePayload)!;
        }
    }


    private class HistoryState
    {
        public required int RatingAttributeId { get; set; }
        public required bool IncludeGameHistory { get; set; }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static HistoryState Deserialize(InteractionState state)
        {
            return JsonSerializer.Deserialize<HistoryState>(state.StatePayload)!;
        }
    }
}