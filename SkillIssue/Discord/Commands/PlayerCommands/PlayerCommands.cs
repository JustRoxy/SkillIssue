using System.Text;
using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using osu.Framework.Extensions;
using osu.Game.Rulesets.Scoring;
using PlayerPerformanceCalculator.Services;
using SkillIssue.Database;
using SkillIssue.Discord.Extensions;
using SkillIssue.Domain.Discord;
using SkillIssue.Domain.PPC.Entities;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;
using TheGreatSpy.Services;
using Unfair.Strategies;
using Unfair.Strategies.Modification;
using Unfair.Strategies.Selection;

//TODO: properly refactor this
namespace SkillIssue.Discord.Commands.PlayerCommands;

[Group("player", "Player commands")]
public class PlayerCommands(
    DatabaseContext context,
    ILogger<PlayerCommands> logger,
    IOpenSkillCalculator calculator,
    PlayerService playerService,
    ScoreProcessing scoreProcessing)
    : CommandBase<PlayerCommands>
{
    private static readonly Emoji SmirkCatEmoji = Emoji.Parse(":smirk_cat:");
    private static readonly Emoji BarChartEmoji = Emoji.Parse(":bar_chart:");

    private static readonly Emoji TrackPrevious = new("\u23ee\ufe0f");
    private static readonly Emoji TrackNext = new("\u23ed\ufe0f");

    private static readonly Emoji LeftArrow = new("\u2b05\ufe0f");
    private static readonly Emoji RightArrow = new("\u27a1\ufe0f");
    protected override ILogger<PlayerCommands> Logger { get; } = logger;

    private MessageComponent GenerateButtons(PredictionState state)
    {
        var basicMenuButton = new ButtonBuilder()
            .WithDisabled(state.SelectedMenu == PredictionMenu.Basic)
            .WithEmote(SmirkCatEmoji)
            .WithLabel("basic")
            .WithStyle(ButtonStyle.Secondary)
            .WithCustomId($"player.predict-{PredictionMenu.Basic}");

        var inDepthButton = new ButtonBuilder()
            .WithDisabled(state.SelectedMenu == PredictionMenu.InDepth)
            .WithEmote(BarChartEmoji)
            .WithLabel("in-depth")
            .WithStyle(ButtonStyle.Secondary)
            .WithCustomId($"player.predict-{PredictionMenu.InDepth}");

        var builder = new ComponentBuilder()
            .WithButton(basicMenuButton)
            .WithButton(inDepthButton);

        if (state.SelectedMenu == PredictionMenu.InDepth)
        {
            var menu = new SelectMenuBuilder()
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithCustomId("player.predict.attrib-mod");

            foreach (var mod in Enum.GetValues<ModificationRatingAttribute>())
                menu.AddOption(RatingAttribute.DescriptionFormat(mod),
                    mod.ToString(),
                    isDefault: mod == state.SelectedMod);

            builder.WithSelectMenu(menu);
        }

        return builder.Build();
    }

    private Task<Embed> GenerateEmbed(PredictionState state)
    {
        return state.SelectedMenu switch
        {
            PredictionMenu.Basic => GenerateBasicOneOnOneEmbed(state),
            PredictionMenu.InDepth => GenerateInDepthOneOnOneEmbed(state),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<Embed> GenerateInDepthOneOnOneEmbed(PredictionState state)
    {
        var selectedMod = state.SelectedMod;
        var player1 = await context.Players.AsNoTracking()
            .Select(x => new Player
            {
                ActiveUsername = x.ActiveUsername,
                PlayerId = x.PlayerId,
                AvatarUrl = x.AvatarUrl
            })
            .FirstAsync(x => x.PlayerId == state.LeftPlayer);

        var player2 = await context.Players.AsNoTracking()
            .Select(x => new Player
            {
                ActiveUsername = x.ActiveUsername,
                PlayerId = x.PlayerId,
                AvatarUrl = x.AvatarUrl
            })
            .FirstAsync(x => x.PlayerId == state.RightPlayer);

        var ratings1 = await context.Ratings
            .AsNoTracking()
            .Where(x => x.RatingAttribute.Modification == selectedMod)
            .Include(x => x.RatingAttribute)
            .Where(x => x.PlayerId == state.LeftPlayer)
            .ToListAsync();

        var ratings2 = await context.Ratings
            .AsNoTracking()
            .Where(x => x.RatingAttribute.Modification == selectedMod)
            .Include(x => x.RatingAttribute)
            .Where(x => x.PlayerId == state.RightPlayer)
            .ToListAsync();

        var embed = new EmbedBuilder()
            .WithTitle(
                $"{player1.ActiveUsername} vs {player2.ActiveUsername}")
            .WithDescription(
                $"In-Depth predictions on {state.SelectedMod.ToEmote()} {Format.Bold(RatingAttribute.DescriptionFormat(state.SelectedMod))}");

        if (ratings1.Count == 0 || ratings2.Count == 0)
        {
            embed
                .WithThumbnailUrl(player1.AvatarUrl)
                .AddField("Predictions can't be made", $"No ratings on this mod {SmirkCatEmoji}");
            return embed.Build();
        }

        var globalRating1 = ratings1.First(x => x.RatingAttribute.Skillset == SkillsetRatingAttribute.Overall);
        var globalRating2 = ratings2.First(x => x.RatingAttribute.Skillset == SkillsetRatingAttribute.Overall);

        var results = calculator.PredictWinHeadOnHead(globalRating1, globalRating2);

        var winnerAvatar = results[0] > results[1] ? player1.AvatarUrl : player2.AvatarUrl;

        embed
            .WithThumbnailUrl(winnerAvatar)
            .AddField(player1.ActiveUsername, FormatPrediction(results[0], "P3"), true)
            .AddField(player2.ActiveUsername, FormatPrediction(results[1], "P3"), true);

        var groups = ratings1.Union(ratings2)
            .OrderBy(x => x.RatingAttributeId)
            .GroupBy(x => x.RatingAttribute.Skillset)
            .Where(x => x.DistinctBy(z => z.PlayerId).Count() > 1)
            .Select(x => new
            {
                Skillset = x.Key,

                LeftPlayerScore = x
                    .Where(z => z.PlayerId == player1.PlayerId)
                    .First(z => z.RatingAttribute.Scoring == ScoringRatingAttribute.Score),
                LeftPlayerAccuracy = x
                    .Where(z => z.PlayerId == player1.PlayerId)
                    .First(z => z.RatingAttribute.Scoring == ScoringRatingAttribute.Accuracy),
                LeftPlayerCombo = x
                    .Where(z => z.PlayerId == player1.PlayerId)
                    .First(z => z.RatingAttribute.Scoring == ScoringRatingAttribute.Combo),
                LeftPlayerPps = x
                    .Where(z => z.PlayerId == player1.PlayerId)
                    .FirstOrDefault(z => z.RatingAttribute.Scoring == ScoringRatingAttribute.PP)
                    ?.Ordinal,
                RightPlayerScore = x
                    .Where(z => z.PlayerId == player2.PlayerId)
                    .First(z => z.RatingAttribute.Scoring == ScoringRatingAttribute.Score),
                RightPlayerAccuracy = x
                    .Where(z => z.PlayerId == player2.PlayerId)
                    .First(z => z.RatingAttribute.Scoring == ScoringRatingAttribute.Accuracy),
                RightPlayerCombo = x
                    .Where(z => z.PlayerId == player2.PlayerId)
                    .First(z => z.RatingAttribute.Scoring == ScoringRatingAttribute.Combo),
                RightPlayerPps = x
                    .Where(z => z.PlayerId == player2.PlayerId)
                    .FirstOrDefault(z => z.RatingAttribute.Scoring == ScoringRatingAttribute.PP)
                    ?.Ordinal
            })
            .Select(x => new
            {
                x.Skillset,
                ScorePrediction = calculator.PredictWinHeadOnHead(x.LeftPlayerScore, x.RightPlayerScore)[0],
                AccuracyPrediction = calculator.PredictWinHeadOnHead(x.LeftPlayerAccuracy, x.RightPlayerAccuracy)[0],
                ComboPrediction = calculator.PredictWinHeadOnHead(x.LeftPlayerCombo, x.RightPlayerCombo)[0],
                x.LeftPlayerPps,
                x.RightPlayerPps
            })
            .ToList();

        var overallPrediction = groups.First(x => x.Skillset == SkillsetRatingAttribute.Overall);
        embed.AddField("Accuracy", overallPrediction.AccuracyPrediction.ToString("P2"));
        embed.AddField("Combo", overallPrediction.ComboPrediction.ToString("P2"));

        foreach (var group in groups.Where(x => x.Skillset != SkillsetRatingAttribute.Overall)
                     .OrderByDescending(x => x.ScorePrediction))
        {
            var firstPlayerPp = group.LeftPlayerPps ?? 0;
            var secondPlayerPp = group.RightPlayerPps ?? 0;
            var content =
                $"""
                 Score: {FormatPrediction(group.ScorePrediction, "P2")}
                 Accuracy: {FormatPrediction(group.AccuracyPrediction, "P2")}
                 Combo: {FormatPrediction(group.ComboPrediction, "P2")}
                 PP: {firstPlayerPp} vs {secondPlayerPp}
                 """;

            embed.AddField($"{group.Skillset.ToEmote()} {RatingAttribute.DescriptionFormat(group.Skillset)}", content);
        }

        return embed.Build();
    }

    private string GenerateComboGameTitle(Rating player1Combo,
        Rating player1Accuracy,
        Rating player2Combo,
        Rating player2Accuracy)
    {
        var player1AccuracyPredictions = calculator.PredictWinHeadOnHead(player1Accuracy, player2Accuracy)[0];
        var player1ComboPredictions = calculator.PredictWinHeadOnHead(player1Combo, player2Combo)[0];

        var accuracy = player1AccuracyPredictions > 0.5 ? "better accuracy" : "worse accuracy";
        var combo = player1ComboPredictions > 0.5 ? "better combo" : "worse combo";

        return $"{accuracy} and {combo}";
    }

    private async Task<Embed> GenerateBasicOneOnOneEmbed(PredictionState state)
    {
        var player1 = await context.Players.AsNoTracking()
            .Select(x => new Player
            {
                ActiveUsername = x.ActiveUsername,
                PlayerId = x.PlayerId,
                AvatarUrl = x.AvatarUrl
            })
            .FirstAsync(x => x.PlayerId == state.LeftPlayer);

        var player2 = await context.Players.AsNoTracking()
            .Select(x => new Player
            {
                ActiveUsername = x.ActiveUsername,
                PlayerId = x.PlayerId,
                AvatarUrl = x.AvatarUrl
            })
            .FirstAsync(x => x.PlayerId == state.RightPlayer);

        var ratings1 = await context.Ratings
            .AsNoTracking()
            .Major()
            .Include(x => x.RatingAttribute)
            .Where(x => x.PlayerId == state.LeftPlayer)
            .ToListAsync();

        var ratings2 = await context.Ratings
            .AsNoTracking()
            .Major()
            .Include(x => x.RatingAttribute)
            .Where(x => x.PlayerId == state.RightPlayer)
            .ToListAsync();

        var embed = new EmbedBuilder()
            .WithTitle("Predictions");

        if (ratings1.Count == 0 || ratings2.Count == 0)
        {
            embed
                .WithThumbnailUrl(player1.AvatarUrl)
                .AddField("Predictions can't be made", $"No ratings {SmirkCatEmoji}");
            return embed.Build();
        }

        var globalRating1 = ratings1.First(x => x.RatingAttributeId == 0);
        var globalRating2 = ratings2.First(x => x.RatingAttributeId == 0);

        var results = calculator.PredictWinHeadOnHead(globalRating1, globalRating2);

        var accuracyCombo = GenerateComboGameTitle(ratings1.First(x => x.RatingAttributeId == 1),
            ratings1.First(x => x.RatingAttributeId == 2), ratings2.First(x => x.RatingAttributeId == 1),
            ratings2.First(x => x.RatingAttributeId == 2));

        var winnerAvatar = results[0] > results[1] ? player1.AvatarUrl : player2.AvatarUrl;

        embed
            .WithThumbnailUrl(winnerAvatar)
            .WithFooter($"You will have {accuracyCombo}")
            .AddField(player1.ActiveUsername, FormatPrediction(results[0], "P3"), true)
            .AddField(player2.ActiveUsername, FormatPrediction(results[1], "P3"), true);

        var groups = ratings1.Union(ratings2)
            .Where(x => x.RatingAttribute.Scoring == ScoringRatingAttribute.Score)
            .Where(x => x.RatingAttribute is not
                { Modification: ModificationRatingAttribute.AllMods, Skillset: SkillsetRatingAttribute.Overall })
            .GroupBy(x => x.RatingAttributeId)
            .Where(x => x.Count() > 1)
            .Select(group =>
            {
                var rating1 = group.First(y => y.PlayerId == player1.PlayerId);
                var rating2 = group.First(y => y.PlayerId == player2.PlayerId);
                var predictions = calculator.PredictWinHeadOnHead(rating1, rating2);

                return new
                {
                    rating1.RatingAttribute,
                    FirstPlayerPrediction = predictions[0],
                    SecondPlayerPredictions = predictions[1]
                };
            })
            .OrderByDescending(x => x.FirstPlayerPrediction);

        var modifications = new StringBuilder();
        var skillsets = new StringBuilder();

        foreach (var group in groups)
        {
            var skillset = group.RatingAttribute.Skillset;
            var mod = group.RatingAttribute.Modification;

            if (skillset == SkillsetRatingAttribute.Overall)
            {
                var title = $"{mod.ToEmote()} {RatingAttribute.DescriptionFormat(mod)}";
                if (Threshold(group.FirstPlayerPrediction))
                    modifications.AppendLine($"{title}: {FormatPrediction(group.FirstPlayerPrediction, "P0")}");
            }
            else
            {
                var title = $"{skillset.ToEmote()} {RatingAttribute.DescriptionFormat(skillset)}";
                if (Threshold(group.FirstPlayerPrediction))
                    skillsets.AppendLine($"{title}: {FormatPrediction(group.FirstPlayerPrediction, "P0")}");
            }
        }

        if (modifications.Length == 0 && skillsets.Length == 0)
        {
            embed.AddField("You should pick", "idk");
        }
        else
        {
            if (modifications.Length != 0)
                embed.AddField("You should pick these modifications", modifications.ToString());

            if (skillsets.Length != 0) embed.AddField("You should pick these skillsets", skillsets.ToString());
        }

        return embed.Build();
    }

    [ComponentInteraction("player.predict.attrib-*", true)]
    public async Task HandleMenu(string _, string[] selected)
    {
        await Catch(async () =>
        {
            var component = (SocketMessageComponent)Context.Interaction;
            var interaction = await context.Interactions.FindAsync(component.Message.Id);
            if (interaction is null || !await CheckUserId(interaction)) return;

            await DeferAsync();

            var nextMod = Enum.Parse<ModificationRatingAttribute>(selected[0]);
            var state = PredictionState.Deserialize(interaction);
            state.SelectedMod = nextMod;

            var embed = await GenerateEmbed(state);

            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed;
                x.Components = GenerateButtons(state);
            });

            interaction.StatePayload = state.Serialize();
            await context.SaveChangesAsync();
        });
    }

    [ComponentInteraction("player.predict-*", true)]
    public async Task HandleButton(string customId)
    {
        await Catch(async () =>
        {
            var component = (SocketMessageComponent)Context.Interaction;
            var interaction = await context.Interactions.FindAsync(component.Message.Id);
            if (interaction is null || !await CheckUserId(interaction)) return;

            await DeferAsync();

            var nextMenu = Enum.Parse<PredictionMenu>(customId);
            var state = PredictionState.Deserialize(interaction);
            state.SelectedMenu = nextMenu;

            var embed = await GenerateEmbed(state);

            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed;
                x.Components = GenerateButtons(state);
            });

            interaction.StatePayload = state.Serialize();
            await context.SaveChangesAsync();
        });
    }

    [SlashCommand("predict", "One on One prediction")]
    public async Task Predict1V1(string yourUsername, string opponentUsername)
    {
        await Catch(async () =>
        {
            await DeferAsync();

            var player1 = await HandlePlayerRequest(yourUsername, playerService);
            if (player1 == default) return;

            var player2 = await HandlePlayerRequest(opponentUsername, playerService);
            if (player2 == default) return;

            var state = new PredictionState
            {
                LeftPlayer = player1.PlayerId,
                RightPlayer = player2.PlayerId,
                SelectedMod = ModificationRatingAttribute.AllMods,
                SelectedMenu = PredictionMenu.Basic
            };

            var message = await FollowupAsync(embed: await GenerateBasicOneOnOneEmbed(state),
                components: GenerateButtons(state));
            var interactionState = new InteractionState
            {
                CreatorId = Context.User.Id,
                MessageId = message.Id,
                PlayerId = null,
                CreationTime = DateTime.UtcNow,
                StatePayload = state.Serialize()
            };

            context.Interactions.Add(interactionState);

            await context.SaveChangesAsync();
        });
    }

    private string FormatPrediction(double prediction, string format)
    {
        var f = prediction.ToString(format);
        if (prediction > 0.5d) return Format.Bold(f);
        return f;
    }

    [SlashCommand("scores", "Get player's tournament scores")]
    private async Task GetScores(string username)
    {
        await Catch(async () =>
        {
            await DeferAsync();

            var player = await HandlePlayerRequest(username, playerService);
            if (player == default) return;

            var state = new GetScoresState
            {
                Ordering = GetScoresStateOrdering.Pp,
                Page = 0
            };

            var (embed, component) = await HandleGetScores(state, player);
            var message = await FollowupAsync(embed: embed, components: component);

            var interaction = new InteractionState
            {
                CreatorId = Context.User.Id,
                PlayerId = player.PlayerId,
                CreationTime = DateTime.UtcNow,
                MessageId = message.Id,
                StatePayload = state.Serialize()
            };
            context.Interactions.Add(interaction);
            await context.SaveChangesAsync();
        });
    }

    [ComponentInteraction("player_scores.pager-*", true)]
    public async Task PlayerScoresButtonComponent(string id)
    {
        await Catch(async () =>
        {
            var component = (SocketMessageComponent)Context.Interaction;
            var interaction = await context.Interactions.FindAsync(component.Message.Id);
            if (interaction is null || !await CheckUserId(interaction)) return;

            await DeferAsync();

            var state = GetScoresState.Deserialize(interaction);

            if (id == "ordering")
            {
                state.Ordering = Enum.Parse<GetScoresStateOrdering>(component.Data.Values.First());
            }
            else
            {
                var button = Enum.Parse<GetScoresButtons>(id);

                switch (button)
                {
                    case GetScoresButtons.First:
                        state.Page = 0;
                        break;
                    case GetScoresButtons.Previous:
                        state.Page--;
                        break;
                    case GetScoresButtons.Next:
                        state.Page++;
                        break;
                    case GetScoresButtons.Last:
                        state.Page = state.LastPage;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var player = await playerService.GetPlayerById(interaction.PlayerId!.Value);
            var (embed, nextComponent) = await HandleGetScores(state, player!);

            await ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed;
                x.Components = nextComponent;
            });

            interaction.StatePayload = state.Serialize();
            await context.SaveChangesAsync();
        });
    }

    private async Task<(Embed embed, MessageComponent component)> HandleGetScores(GetScoresState state, Player player)
    {
        const int scoresPerPage = 10;
        var embed = new EmbedBuilder()
            .WithTitle($"{player.ActiveUsername} scores by PP")
            .WithThumbnailUrl(player.AvatarUrl)
            .WithUrl(player.GetUrl());

        var playerTotalPp = await context.Ratings
            //PP rating
            .Where(x => x.RatingAttributeId == 3 && x.PlayerId == player.PlayerId)
            .Select(x => x.Ordinal)
            .FirstOrDefaultAsync();

        var footerIndexes = $"| Total: {playerTotalPp:N0}pp";

        if (player.Pp is not null && player.Pp != 0)
        {
            footerIndexes += $", deranker_index: {playerTotalPp / player.Pp:P2}";
        }

        var query = context.Scores
            .AsNoTracking()
            .Where(x => x.PlayerId == player.PlayerId)
            .Where(x => x.Pp != null);

        var totalCount = await query.CountAsync();
        query = state.Ordering switch
        {
            GetScoresStateOrdering.Pp => query.OrderByDescending(x => x.Pp),
            GetScoresStateOrdering.Date => query.OrderByDescending(x => x.MatchId),
            _ => throw new ArgumentOutOfRangeException()
        };

        var scores = await query
            .Skip(state.Page * scoresPerPage)
            .Take(scoresPerPage)
            .Select(x => new
            {
                x.Match.Name,
                x.Match.StartTime,
                x.MatchId,
                x.Pp,
                x.BeatmapId,
                x.LegacyMods,
                x.Accuracy,
                x.Count300,
                x.Count100,
                x.Count50,
                x.CountMiss,
                Beatmap = new Beatmap
                {
                    Artist = x.Beatmap!.Artist,
                    Name = x.Beatmap.Name,
                    Version = x.Beatmap.Version
                }
            })
            .ToListAsync();

        foreach (var score in scores)
        {
            var grade = scoreProcessing.GetGrade(score.Accuracy, score.LegacyMods, new Dictionary<HitResult, int>
            {
                {
                    HitResult.Great, score.Count300
                },
                {
                    HitResult.Good, score.Count100
                },
                {
                    HitResult.Meh, score.Count50
                },
                {
                    HitResult.Miss, score.CountMiss
                }
            });

            var beatmap = score.Beatmap.FullName;
            var beatmapFormat = score.BeatmapId is null
                ? beatmap
                : Format.Url(beatmap, $"https://osu.ppy.sh/b/{score.BeatmapId}");

            var acronym = string.Join("", scoreProcessing.GetModificationAcronym(score.LegacyMods)
                .Where(x => x != "NF"));
            var mods = string.IsNullOrEmpty(acronym)
                ? ""
                : $" +{acronym}";

            var date = GetRelativeTime(score.StartTime.Date);
            var statistics =
                Format.Subtext(
                    $"{grade.GetDescription()} [{score.Count300}/{score.Count100}/{score.Count50}/{score.CountMiss}] {mods}");
            embed.AddField($"{Format.Bold(score.Pp!.Value.ToString("N0") + $"pp [{date}]")} ",
                $"{Format.Bold(beatmapFormat)}\n{statistics}\nMatch: {Format.Url(score.Name, $"https://osu.ppy.sh/mp/{score.MatchId}")}");
        }

        var totalPages = totalCount / scoresPerPage;
        if (totalCount != 0 && totalCount % scoresPerPage == 0) totalPages--;
        state.LastPage = totalPages;

        embed.WithFooter($"Page {state.Page + 1} / {totalPages + 1} {footerIndexes}");

        #region Pager

        var firstButton = new ButtonBuilder()
            .WithCustomId($"player_scores.pager-{GetScoresButtons.First}")
            .WithDisabled(state.Page == 0)
            .WithEmote(TrackPrevious)
            .WithStyle(ButtonStyle.Primary);

        var previousPageButton = new ButtonBuilder()
            .WithCustomId($"player_scores.pager-{GetScoresButtons.Previous}")
            .WithDisabled(state.Page == 0)
            .WithEmote(LeftArrow)
            .WithStyle(ButtonStyle.Primary);

        var nextPageButton = new ButtonBuilder()
            .WithCustomId($"player_scores.pager-{GetScoresButtons.Next}")
            .WithDisabled(state.Page == totalPages)
            .WithEmote(RightArrow)
            .WithStyle(ButtonStyle.Primary);

        var lastButton = new ButtonBuilder()
            .WithCustomId($"player_scores.pager-{GetScoresButtons.Last}")
            .WithDisabled(state.Page == totalPages)
            .WithEmote(TrackNext)
            .WithStyle(ButtonStyle.Primary);

        var choices = new SelectMenuBuilder()
            .AddOption("PP", "Pp", "Order by PP", isDefault: state.Ordering == GetScoresStateOrdering.Pp)
            .AddOption("Date", "Date", "Order by date", isDefault: state.Ordering == GetScoresStateOrdering.Date)
            .WithCustomId("player_scores.pager-ordering")
            .WithMinValues(1).WithMaxValues(1);

        var menuBuilder = new ComponentBuilder()
            .WithButton(firstButton)
            .WithButton(previousPageButton)
            .WithButton(nextPageButton)
            .WithButton(lastButton)
            .WithSelectMenu(choices);

        #endregion

        return (embed.Build(), menuBuilder.Build());
    }

    private static string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalSeconds < 60)
            return FormatTime((int)timeSpan.TotalSeconds, "second");
        else if (timeSpan.TotalMinutes < 60)
            return FormatTime((int)timeSpan.TotalMinutes, "minute");
        else if (timeSpan.TotalHours < 24)
            return FormatTime((int)timeSpan.TotalHours, "hour");
        else if (timeSpan.TotalDays < 30)
            return FormatTime((int)timeSpan.TotalDays, "day");
        else if (timeSpan.TotalDays < 365)
            return FormatTime((int)(timeSpan.TotalDays / 30), "month");
        else
            return FormatTime((int)(timeSpan.TotalDays / 365), "year");

        static string FormatTime(int value, string unit)
        {
            return value == 1 ? $"1 {unit} ago" : $"{value} {unit}s ago";
        }
    }

    [SlashCommand("compare", "Compare players")]
    public async Task Compare(string yourUsername, string opponentUsername)
    {
        await Catch(async () =>
        {
            await DeferAsync();

            var player1 = await HandlePlayerRequest(yourUsername, playerService);
            if (player1 == default) return;

            var player2 = await HandlePlayerRequest(opponentUsername, playerService);
            if (player2 == default) return;

            var embed = new EmbedBuilder()
                .WithTitle($"Indirect Matchups against {player2.ActiveUsername}")
                .WithThumbnailUrl(player2.AvatarUrl)
                .WithAuthor(player1.ActiveUsername, player1.AvatarUrl);
            var indirectMatchups = (await context.Scores
                    .Include(x => x.Beatmap)
                    .AsNoTrackingWithIdentityResolution()
                    .Where(x => x.PlayerId == player1.PlayerId || x.PlayerId == player2.PlayerId)
                    .Where(x => x.BeatmapId != null)
                    .Where(x => x.ScoringType == ScoringType.ScoreV2)
                    .GroupBy(x => new
                    {
                        x.BeatmapId,
                        Mod = x.LegacyMods & ~TrimmingModificationStrategy.UselessMods
                    })
                    .Where(x => x.Select(z => z.PlayerId).Distinct().Count() > 1)
                    .Select(x => new
                    {
                        x.Key.Mod,
                        Player1Score = x.OrderByDescending(z => z.Match.StartTime)
                            .First(z => z.PlayerId == player1.PlayerId),
                        Player2Score = x.OrderByDescending(z => z.Match.StartTime)
                            .First(z => z.PlayerId == player2.PlayerId)
                    })
                    .ToListAsync())
                .GroupBy(
                    x => DefaultModificationSelectionStrategy.Instance.Select(x.Mod).Last())
                .Where(x => x.Key != ModificationRatingAttribute.AllMods)
                .OrderBy(x => x.Key)
                .Select(x => new
                {
                    Mod = x.Key,
                    Scores = x.Select(z => new
                    {
                        z.Player1Score,
                        z.Player2Score,
                        Winner = z.Player1Score.TotalScore > z.Player2Score.TotalScore ? z.Player1Score : z.Player2Score
                    })
                })
                .ToList();

            string FormatMatchup(int p1, int p2)
            {
                return p1 == p2 ? $"{p1} - {p2}"
                    : p1 > p2 ? $"{Format.Bold(p1.ToString())} - {p2}"
                    : $"{p1} - {Format.Bold(p2.ToString())}";
            }

            var player1Total = 0;
            var player2Total = 0;


            var builder =
                new StringBuilder($"Indirect matchups for {player1.ActiveUsername} vs {player2.ActiveUsername}\n");

            foreach (var indirectMatchup in indirectMatchups)
            {
                var modFormat = RatingAttribute.DescriptionFormat(indirectMatchup.Mod);
                builder.AppendLine($"Matchups for {modFormat}");
                var player1ModWin = indirectMatchup.Scores.Count(z => z.Winner.PlayerId == player1.PlayerId);
                var player2ModWin = indirectMatchup.Scores.Count(z => z.Winner.PlayerId == player2.PlayerId);
                player1Total += player1ModWin;
                player2Total += player2ModWin;
                embed.AddField(
                    $"{indirectMatchup.Mod.ToEmote()} {modFormat}",
                    FormatMatchup(player1ModWin, player2ModWin));

                foreach (var scores in indirectMatchup.Scores)
                {
                    var winner = scores.Player1Score.TotalScore > scores.Player2Score.TotalScore
                        ? player1.ActiveUsername
                        : player2.ActiveUsername;

                    var difference = Math.Abs(scores.Player1Score.TotalScore - scores.Player2Score.TotalScore);

                    builder.AppendLine(
                        $"\t{scores.Player1Score.Beatmap?.FullName}. Winner: {winner} by ({difference})");
                    builder.AppendLine($"\t{player1.ActiveUsername}: {scores.Player1Score.TotalScore}");
                    builder.AppendLine($"\t{player2.ActiveUsername}: {scores.Player2Score.TotalScore}");
                }
            }

            embed.WithDescription($"Total: {FormatMatchup(player1Total, player2Total)}");

            await FollowupWithFileAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString())),
                $"matchups-{player1.ActiveUsername}-vs-{player2.ActiveUsername}.txt",
                embed: embed.Build()
            );
        });
    }

    private bool Threshold(double prediction)
    {
        return prediction >= 0.25;
    }

    private enum GetScoresStateOrdering
    {
        Pp,
        Date
    }

    private enum GetScoresButtons
    {
        First,
        Previous,
        Next,
        Last
    }

    private class GetScoresState
    {
        public int Page { get; set; }
        public int LastPage { get; set; }
        public GetScoresStateOrdering Ordering { get; set; }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static GetScoresState Deserialize(InteractionState state)
        {
            return JsonSerializer.Deserialize<GetScoresState>(state.StatePayload)!;
        }
    }

    private enum PredictionMenu
    {
        Basic,
        InDepth
    }

    private class PredictionState
    {
        public required int LeftPlayer { get; set; }
        public required int RightPlayer { get; set; }
        public required PredictionMenu SelectedMenu { get; set; }
        public required ModificationRatingAttribute SelectedMod { get; set; }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static PredictionState Deserialize(InteractionState state)
        {
            return JsonSerializer.Deserialize<PredictionState>(state.StatePayload)!;
        }
    }
}