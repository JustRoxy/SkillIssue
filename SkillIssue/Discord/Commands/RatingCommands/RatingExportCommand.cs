using System.Text;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using SkillIssue.Domain.Extensions;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;
using Unfair.Strategies;

namespace SkillIssue.Discord.Commands.RatingCommands;

[Flags]
public enum ExportOptions
{
    None = 0,
    IncludePP = 1,
    IncludeAccuracyAndCombo = 2,
    IncludeDetailedSkillsets = 4,
    ExcludeUnrankedPlayers = 8
}

[Group("ratings", "Bulk ratings command")]
public class BulkRatingsCommand(
    SpreadsheetProvider spreadsheetProvider,
    DatabaseContext context,
    ILogger<BulkRatingsCommand> logger,
    IOpenSkillCalculator openSkillCalculator)
    : CommandBase<BulkRatingsCommand>
{
    protected override ILogger<BulkRatingsCommand> Logger => logger;

    private async Task<List<string>> ProcessSpreadsheet(string spreadsheet)
    {
        string spreadsheetId;
        string table;
        string range;

        try
        {
            var split = spreadsheet.Split(",").Select(x => x.Trim()).ToArray();
            spreadsheetId = split[0].Split("/")[5];
            table = split[1];
            range = split[2];
        }
        catch (Exception)
        {
            throw new UserInteractionException("Unsupported format of spreadsheet");
        }

        return await spreadsheetProvider.ExtractUsername(spreadsheetId, table, range);
    }

    private async Task ExportRatingsImpl(
        string? usernameString,
        string? spreadsheet,
        ExportOptions exportOptions)
    {
        if (usernameString is null && spreadsheet is null)
            throw new UserInteractionException(
                "Neither usernames nor a spreadsheet arguments were provided");

        var pointsEnum = RatingAttribute.GetAllUsableAttributes().ToList();
        //FIXME: patch for DT/HighAR
        var dtHighAr = pointsEnum.Where(x => x.Skillset == SkillsetRatingAttribute.HighAR).ToList();
        dtHighAr.ForEach(x => { x.Modification = ModificationRatingAttribute.AllMods; });

        if (!exportOptions.HasFlag(ExportOptions.IncludeDetailedSkillsets))
            pointsEnum = pointsEnum.Where(x =>
                    x.Modification == ModificationRatingAttribute.AllMods ||
                    x.Skillset == SkillsetRatingAttribute.Overall)
                .ToList();
        if (!exportOptions.HasFlag(ExportOptions.IncludePP))
            pointsEnum = pointsEnum.Where(x => x.Scoring != ScoringRatingAttribute.PP).ToList();
        if (!exportOptions.HasFlag(ExportOptions.IncludeAccuracyAndCombo))
            pointsEnum = pointsEnum.Where(x =>
                x.Scoring is not (ScoringRatingAttribute.Accuracy or ScoringRatingAttribute.Combo)).ToList();

        pointsEnum = pointsEnum.GroupBy(x => x.Scoring)
            .OrderBy(x => x.Key)
            .SelectMany(x => x.OrderBy(z =>
            {
                //FIXME: related to previous FIXME
                if (z.Skillset == SkillsetRatingAttribute.HighAR)
                    return RatingAttribute.GetAttribute(ModificationRatingAttribute.AllMods,
                        SkillsetRatingAttribute.HighAR,
                        z.Scoring).AttributeId;
                return z.AttributeId;
            }))
            .ToList();

        var points = pointsEnum.ToList();

        var usernames = usernameString?.Split(",").Select(x => x.Trim()).ToList() ?? [];

        if (spreadsheet is not null) usernames.AddRange((await ProcessSpreadsheet(spreadsheet)).Where(IsCorrectUsername));

        usernames = usernames.DistinctBy(Player.NormalizeUsername, StringComparer.InvariantCultureIgnoreCase).Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        var playerList = await GetPlayerIdsFromUsernames(usernames).ToListAsync();

        var attributeIds = points.Select(x => x.AttributeId).ToList();

        var players = playerList.Where(x => x.player is not null).ToList();

        var playerIds = players.Select(x => x.player!.PlayerId).ToList();
        var ratings = await context.Ratings
            .AsNoTracking()
            .Where(x => playerIds.Contains(x.PlayerId) && attributeIds.Contains(x.RatingAttributeId))
            .Case(exportOptions.HasFlag(ExportOptions.ExcludeUnrankedPlayers), query => query.Ranked())
            .Select(x => new
            {
                x.RatingAttributeId,
                x.Mu,
                x.Sigma,
                x.Player.PlayerId,
                x.Player.ActiveUsername,
                x.StarRating,
                x.Ordinal
            })
            .ToDictionaryAsync(x => (x.PlayerId, x.RatingAttributeId));

        var missingUsernames = playerList.Where(x => x.player is null).Select(x => x.requestedUsername).ToList();
        var modHeaders = string.Join(",", points.Select(RatingAttribute.GetCsvHeaderValue));
        StringBuilder ratingBuilder = new($"username,{modHeaders}\n");

        foreach (var (requestedUsername, player) in players)
        {
            if (!ratings.ContainsKey((player!.PlayerId, 0)))
            {
                missingUsernames.Add(requestedUsername);
                continue;
            }

            ratingBuilder.Append($"{player.ActiveUsername}");

            foreach (var point in points)
            {
                var rating = ratings.GetValueOrDefault((player.PlayerId, point.AttributeId));
                var ordinal = 0d;
                if (rating is not null) ordinal = rating.Ordinal;
                ratingBuilder.Append($",{ordinal:F0}");
            }

            ratingBuilder.AppendLine();
        }

        var predictionBuilder = new StringBuilder();

        var ratingGroups = ratings.GroupBy(x => x.Key.RatingAttributeId, x => (x.Key.Item1, x.Value)).ToList();

        foreach (var point in ratingGroups
                     .OrderBy(x => x.Key)
                     .Where(x => !RatingAttribute.IsAttributeSet(x.Key, ScoringRatingAttribute.PP)))
        {
            var (modification, skillset, scoring) = RatingAttribute.GetAttributesFromId(point.Key);
            var attribute = new RatingAttribute
            {
                Modification = modification,
                Skillset = skillset,
                Scoring = scoring
            };

            predictionBuilder.AppendLine($"Predictions for {attribute.Description}");

            var prediction = openSkillCalculator.PredictRankHeadOnHead(point.Select(x => new Rating
            {
                RatingAttributeId = point.Key,
                PlayerId = 0,
                Mu = x.Value.Mu,
                Sigma = x.Value.Sigma,
                StarRating = x.Value.StarRating
            }).ToArray());

            foreach (var (rank, rating) in prediction.Zip(point).OrderBy(x => x.First.rank))
            {
                var username = players.First(x => x.player!.PlayerId == rating.Item1);
                predictionBuilder.AppendLine($"{rank.rank}: {username.player!.ActiveUsername} [{rank.prediction:P}]");
            }

            predictionBuilder.AppendLine();
        }

        var files = new List<FileAttachment>
        {
            new(new MemoryStream(Encoding.UTF8.GetBytes(ratingBuilder.ToString())), "ratings.csv", "Player ratings")
        };

        files.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(predictionBuilder.ToString())),
            "predictions.txt"));

        string missingMsg = missingUsernames.Count switch
        {
            0 => "",
            > 40 => $"{missingUsernames.Count} potential users were not found, but it might be just non-usernames.",
            _ => $"Missing: {string.Join(", ", missingUsernames)}"
        };

        await FollowupWithFilesAsync(files,
            $"Ratings and predictions for {usernames.Count - missingUsernames.Count} players. {missingMsg}");
    }

    private async IAsyncEnumerable<(string requestedUsername, Player? player)> GetPlayerIdsFromUsernames(List<string> usernames)
    {
        foreach (var username in usernames)
        {
            var players = await context.Players
                .AsNoTracking()
                .Select(x => new Player
                {
                    PlayerId = x.PlayerId,
                    ActiveUsername = x.ActiveUsername,
                    Usernames = x.Usernames
                })
                .Where(x => x.Usernames.Any(z => z.NormalizedUsername == username.ToLower()))
                .ToListAsync();

            if (players.Count == 0) yield return (username, null);
            else if (players.Count == 1) yield return (username, players.Single());
            else yield return (username, ResolveNamingConflict(username, players));
        }
    }

    private Player ResolveNamingConflict(string targetUsername, List<Player> players)
    {
        targetUsername = Player.NormalizeUsername(targetUsername);

        var nameHolder = players.FirstOrDefault(x => Player.NormalizeUsername(x.ActiveUsername) == targetUsername);
        if (nameHolder is not null) return nameHolder;

        // in this situation we have multiple players that had the same username, and none of them has the same username as active. Assume the oldest one is the correct one

        return players.MinBy(x => x.PlayerId)!;
    }

    [SlashCommand("export", "Export ratings from google spreadsheet or with usernames")]
    public async Task ExportRatings(
        [Summary(description: "Comma-separated usernames")]
        string? usernames = null,
        [Summary(description: "Google spreadsheet in following format: SpreadsheetUrl,TableName,from:to")]
        string? spreadsheet = null,
        [Summary(description: "Include detailed skillset")]
        bool includeDetailedSkillsets = false,
        [Summary(description: "Include PP statistics")]
        bool includePp = false,
        [Summary(description: "Include Accuracy and Combo statistics")]
        bool includeAccuracyAndCombo = false,
        [Summary(description: "Exclude unranked players")]
        bool excludeUnrankedPlayers = false)

    {
        await Catch(async () =>
        {
            await DeferAsync();

            var flags = ExportOptions.None;
            if (includePp) flags |= ExportOptions.IncludePP;
            if (includeAccuracyAndCombo) flags |= ExportOptions.IncludeAccuracyAndCombo;
            if (includeDetailedSkillsets) flags |= ExportOptions.IncludeDetailedSkillsets;
            if (excludeUnrankedPlayers) flags |= ExportOptions.ExcludeUnrankedPlayers;
            await ExportRatingsImpl(usernames, spreadsheet, flags);
        });
    }

    private static bool IsCorrectUsername(string username)
    {
        return username.Length is >= 2 and <= 20 &&
               // [a-zA-Z0-9]\[\] _-
               username.All(letter => char.IsAsciiLetterOrDigit(letter) || letter == '[' || letter == ']' || letter == ' ' || letter == '_' || letter == '-');
    }
}