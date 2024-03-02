using System.Text;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
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
    IncludeDetailedSkillsets = 4
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

        var pointsEnum = RatingAttribute.GetAllAttributes();

        if (!exportOptions.HasFlag(ExportOptions.IncludeDetailedSkillsets))
            pointsEnum = pointsEnum.Where(x =>
                x.Modification == ModificationRatingAttribute.AllMods || x.Skillset == SkillsetRatingAttribute.Overall);
        if (!exportOptions.HasFlag(ExportOptions.IncludePP))
            pointsEnum = pointsEnum.Where(x => x.Scoring != ScoringRatingAttribute.PP);
        if (!exportOptions.HasFlag(ExportOptions.IncludeAccuracyAndCombo))
            pointsEnum = pointsEnum.Where(x =>
                x.Scoring is not (ScoringRatingAttribute.Accuracy or ScoringRatingAttribute.Combo));

        pointsEnum = pointsEnum.GroupBy(x => x.Scoring)
            .OrderBy(x => x.Key)
            .SelectMany(x => x.OrderBy(z => z.AttributeId));

        var points = pointsEnum.ToList();

        var usernames = usernameString?.Split(",").Select(x => x.Trim()).ToList() ?? [];
        List<string> missingUsernames = [];

        if (spreadsheet is not null) usernames.AddRange(await ProcessSpreadsheet(spreadsheet));

        usernames = usernames
            .Select(x => x.Trim().ToLower())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .DistinctBy(x => x, StringComparer.InvariantCultureIgnoreCase)
            .ToList();


        var attributeIds = points.Select(x => x.AttributeId).ToList();

        var playerList = await context.Players
            .Where(x => usernames.Contains(x.ActiveUsername.ToLower()))
            .Select(x => new
            {
                x.PlayerId,
                x.ActiveUsername
            })
            .ToListAsync();
        var players = playerList.GroupBy(x => x.ActiveUsername)
            .Select(x => x.MaxBy(z => z.PlayerId))
            .Select(x => x!.PlayerId)
            .ToList();

        var ratings = await context.Ratings
            .AsNoTracking()
            .Where(x => players.Contains(x.PlayerId) && attributeIds.Contains(x.RatingAttributeId))
            .Select(x => new
            {
                x.RatingAttributeId,
                x.Mu,
                x.Sigma,
                x.Player.ActiveUsername,
                x.StarRating,
                x.Ordinal
            })
            .ToDictionaryAsync(x => (x.ActiveUsername.ToLower(), x.RatingAttributeId));

        var modHeaders = string.Join(",", points.Select(RatingAttribute.GetCsvHeaderValue));
        StringBuilder ratingBuilder = new($"username,{modHeaders}\n");

        foreach (var username in usernames)
        {
            if (!ratings.TryGetValue((username, 0), out var globalRating))
            {
                missingUsernames.Add(username);
                continue;
            }

            ratingBuilder.Append($"{globalRating.ActiveUsername}");
            foreach (var point in points)
            {
                var rating = ratings.GetValueOrDefault((username, point.AttributeId));
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
            var attribute = new RatingAttribute { Modification = modification, Skillset = skillset, Scoring = scoring };

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
                predictionBuilder.AppendLine($"{rank.rank}: {rating.Item1} [{rank.prediction:P}]");

            predictionBuilder.AppendLine();
        }

        var files = new List<FileAttachment>
        {
            new(new MemoryStream(Encoding.UTF8.GetBytes(ratingBuilder.ToString())), "ratings.csv", "Player ratings")
        };

        files.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(predictionBuilder.ToString())),
            "predictions.txt"));

        var missingMsg = missingUsernames.Count == 0 ? "" : $"Missing: {string.Join(", ", missingUsernames)}";
        if (missingUsernames.Count > 40)
            missingMsg = "A lot of users were not found, but it might be just non-usernames.";

        await FollowupWithFilesAsync(files,
            $"Ratings and predictions for {usernames.Count - missingUsernames.Count} players. {missingMsg}");
    }

    [SlashCommand("export", "Export ratings from google spreadsheet or with usernames")]
    public async Task ExportRatings(
        [Summary(description: "Comma-separated usernames")]
        string? usernames = "",
        [Summary(description: "Google spreadsheet in following format: SpreadsheetUrl,TableName,from:to")]
        string? spreadsheet = "",
        [Summary(description: "Include detailed skillset")]
        bool includeDetailedSkillsets = false,
        [Summary(description: "Include PP statistics")]
        bool includePp = false,
        [Summary(description: "Include Accuracy and Combo statistics")]
        bool includeAccuracyAndCombo = false)

    {
        await Catch(async () =>
        {
            await DeferAsync();

            var flags = ExportOptions.None;
            if (includePp) flags |= ExportOptions.IncludePP;
            if (includeAccuracyAndCombo) flags |= ExportOptions.IncludeAccuracyAndCombo;
            if (includeDetailedSkillsets) flags |= ExportOptions.IncludeDetailedSkillsets;
            await ExportRatingsImpl(usernames, spreadsheet, flags);
        });
    }
}