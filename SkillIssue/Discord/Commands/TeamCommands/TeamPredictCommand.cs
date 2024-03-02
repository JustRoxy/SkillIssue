using System.Text.Json;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using SkillIssue.Discord.Extensions;
using SkillIssue.Domain.Discord;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;
using TheGreatSpy.Services;
using Unfair.Strategies;

namespace SkillIssue.Discord.Commands.TeamCommands;

[Group("team", "Team commands")]
public class TeamPredictCommand(
    DatabaseContext context,
    PlayerService playerService,
    ILogger<TeamPredictCommand> logger,
    IOpenSkillCalculator calculator)
    : CommandBase<TeamPredictCommand>
{
    protected override ILogger<TeamPredictCommand> Logger { get; } = logger;

    private async Task<(Embed, string? warningMessage)> PredictTeamOnTeamBasic(TeamPredictState state)
    {
        var firstTeam = await context.Ratings
            .AsNoTrackingWithIdentityResolution()
            .Include(x => x.Player)
            .Where(x => state.FirstTeamPlayers.Contains(x.PlayerId))
            .Major()
            .ToListAsync();

        var secondTeam = await context.Ratings
            .AsNoTrackingWithIdentityResolution()
            .Include(x => x.Player)
            .Where(x => state.SecondTeamPlayers.Contains(x.PlayerId))
            .Major()
            .ToListAsync();

        string? warningMessage = null;
        var firstTeamOverall = firstTeam.Where(x => x.RatingAttributeId == 0).ToList();
        var secondTeamOverall = secondTeam.Where(x => x.RatingAttributeId == 0).ToList();

        if (firstTeamOverall.Count == 0) return (null, "First team have no players with ratings")!;

        if (secondTeamOverall.Count == 0) return (null, "Second team have no players with ratings")!;

        var teamSize = Math.Min(state.TeamSize, Math.Min(firstTeamOverall.Count, secondTeamOverall.Count));

        if (state.TeamSize > teamSize && firstTeamOverall.Count > secondTeamOverall.Count)
            warningMessage = $"First team have more players with ratings. Changing team size to {teamSize}";

        if (state.TeamSize > teamSize && firstTeamOverall.Count < secondTeamOverall.Count)
            warningMessage = $"Second team have more players with ratings. Changing team size to {teamSize}";

        var firstTeamBestPlayers = FindBestPlayers(firstTeamOverall, teamSize);
        var secondTeamBestPlayers = FindBestPlayers(secondTeamOverall, teamSize);

        var bestPlayer = FindBestPlayers(firstTeamOverall.Union(secondTeamOverall).ToList(), 1)[0];
        var embed = new EmbedBuilder()
            .WithTitle("Matchup predictions")
            .WithColor(Color.Green)
            .WithThumbnailUrl(bestPlayer.Player.AvatarUrl);

        var bestPlayersPrediction =
            calculator.PredictWinTeamOnTeam([firstTeamBestPlayers.ToArray(), secondTeamBestPlayers.ToArray()]);
        embed.AddField("Best players comparison",
            $"{FormatRoster(firstTeamBestPlayers)} {bestPlayersPrediction[0]:P0} | {bestPlayersPrediction[1]:P0} {FormatRoster(secondTeamBestPlayers)}");


        foreach (var ratingAttribute in RatingAttribute.GetAllAttributes()
                     .Where(x => x.IsMajor)
                     .Where(x => x.Scoring == ScoringRatingAttribute.Score)
                     .Where(x => x.AttributeId != 0)
                     .OrderBy(x => x.Skillset)
                     .ThenBy(x => x.Modification))
        {
            var firstTeamPlayers =
                FindBestPlayers(firstTeam.Where(x => x.RatingAttributeId == ratingAttribute.AttributeId).ToList(),
                    teamSize);
            var secondTeamPlayers =
                FindBestPlayers(secondTeam.Where(x => x.RatingAttributeId == ratingAttribute.AttributeId).ToList(),
                    teamSize);
            if (firstTeamPlayers.Count == 0 || secondTeamPlayers.Count == 0) continue;
            var prediction =
                calculator.PredictWinTeamOnTeam([firstTeamPlayers.ToArray(), secondTeamPlayers.ToArray()]);
            var mod = ratingAttribute.Skillset == SkillsetRatingAttribute.Overall
                ? RatingAttribute.DescriptionFormat(ratingAttribute.Modification)
                : RatingAttribute.DescriptionFormat(ratingAttribute.Skillset);

            embed.AddField($"{ratingAttribute.ToEmote()} {mod}",
                $"{FormatRoster(firstTeamPlayers)} {prediction[0]:P0} | {prediction[1]:P0} {FormatRoster(secondTeamPlayers)}");
        }

        return (embed.Build(), warningMessage);
    }

    private string FormatRoster(List<Rating> ratings)
    {
        return string.Join(", ", ratings.Select(x => x.Player.ActiveUsername));
    }

    private List<Rating> FindBestPlayers(List<Rating> ratings, int teamSize)
    {
        if (ratings.Count == 0) return [];
        return calculator.PredictRankHeadOnHead(ratings.ToArray()).Zip(ratings)
            .OrderByDescending(x => x.First.prediction)
            .Select(x => x.Second)
            .Take(teamSize)
            .ToList();
    }

    private Task<(Embed, string? warningMessage)> PredictTeamOnTeamImpl(TeamPredictState state)
    {
        if (state.SelectedMenu == TeamPredictMenus.Basic) return PredictTeamOnTeamBasic(state);
        throw new NotImplementedException();
    }

    [SlashCommand("predict", "Predicts rosters and win probability")]
    public async Task PredictTeamOnTeam(
        [Summary(description: "Player usernames separated by comma")]
        string firstTeam,
        [Summary(description: "Player usernames separated by comma")]
        string secondTeam,
        [Summary(description: "Required team size")]
        int? teamSize = null)
    {
        await Catch(async () =>
        {
            await DeferAsync();
            var firstTeamPlayersUsernames = firstTeam.Split(",").Select(x => x.Trim()).ToList();
            var secondTeamPlayersUsernames = secondTeam.Split(",").Select(x => x.Trim()).ToList();


            List<Player> firstTeamPlayers = [];
            List<Player> secondTeamPlayers = [];
            foreach (var username in firstTeamPlayersUsernames)
            {
                var player = await HandlePlayerRequest(username, playerService);
                if (player is null) return;
                firstTeamPlayers.Add(player);
            }

            foreach (var username in secondTeamPlayersUsernames)
            {
                var player = await HandlePlayerRequest(username, playerService);
                if (player is null) return;
                secondTeamPlayers.Add(player);
            }

            var state = new TeamPredictState
            {
                FirstTeamPlayers = firstTeamPlayers.Select(x => x.PlayerId)
                    .Distinct()
                    .ToArray(),
                SecondTeamPlayers = secondTeamPlayers.Select(x => x.PlayerId)
                    .Distinct()
                    .ToArray(),
                SelectedModification = ModificationRatingAttribute.AllMods,
                SelectedMenu = TeamPredictMenus.Basic,
                TeamSize = 0
            };
            state.TeamSize = teamSize ?? state.FirstTeamPlayers.Length;

            var interaction = new InteractionState
            {
                CreatorId = Context.User.Id,
                MessageId = 0,
                PlayerId = null,
                CreationTime = DateTime.UtcNow,
                StatePayload = state.Serialize()
            };

            context.Interactions.Add(interaction);

            var (embed, message) = await PredictTeamOnTeamImpl(state);
            var response = await FollowupAsync(message, embed: embed);

            interaction.MessageId = response.Id;

            await context.SaveChangesAsync();
        });
    }

    private enum TeamPredictMenus
    {
        Basic,
        InDepth
    }

    private class TeamPredictState
    {
        public required int[] FirstTeamPlayers { get; set; }
        public required int[] SecondTeamPlayers { get; set; }
        public required ModificationRatingAttribute SelectedModification { get; set; }
        public required TeamPredictMenus SelectedMenu { get; set; }

        public required int TeamSize { get; set; }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static TeamPredictState Deserialize(InteractionState state)
        {
            return JsonSerializer.Deserialize<TeamPredictState>(state.StatePayload)!;
        }
    }
}