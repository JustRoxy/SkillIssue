using System.Text;
using System.Text.RegularExpressions;
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
    ExcludeUnrankedPlayers = 8,
    IncludeStarRating = 16,
}

[Group("ratings", "Bulk ratings command")]
public class BulkRatingsCommand(
    SpreadsheetProvider spreadsheetProvider,
    DatabaseContext context,
    ILogger<BulkRatingsCommand> logger,
    IOpenSkillCalculator openSkillCalculator)
    : CommandBase<BulkRatingsCommand>
{
    private static readonly Regex DefaultUsernameRegex =
        new(@"(?'username'[a-zA-Z0-9_\-\[\] ]{2,20})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
        Regex? spreadsheetUsernameRegex,
        ExportOptions exportOptions)
    {
        spreadsheetUsernameRegex ??= DefaultUsernameRegex;
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

        var manualUsernames = usernameString?.Split(",").Select(x => x.Trim()).ToHashSet() ?? [];
        var usernames = new List<string>(manualUsernames);

        HashSet<string> spreadsheetUsernames = [];
        if (!string.IsNullOrWhiteSpace(spreadsheet))
            spreadsheetUsernames = (await ProcessSpreadsheet(spreadsheet))
                .Select(x => MatchUsername(x, spreadsheetUsernameRegex)?.Trim())
                .Where(x => x is not null)
                .ToHashSet()!;

        usernames.AddRange(spreadsheetUsernames);
        usernames = usernames.DistinctBy(Player.NormalizeUsername, StringComparer.InvariantCultureIgnoreCase)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        var playerList = await GetPlayerIdsFromUsernames(usernames).ToListAsync();

        var attributeIds = points.Select(x => x.AttributeId).ToList();

        var players = playerList.Where(x => x.player is not null).DistinctBy(x => x.player!.PlayerId).ToList();

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

        var manualMissing = playerList.Where(x => x.player is null).Select(x => x.requestedUsername).Where(x => manualUsernames.Contains(x)).ToList();
        var spreadsheetMissing = playerList.Where(x => x.player is null).Select(x => x.requestedUsername).Where(x => spreadsheetUsernames.Contains(x)).ToList();

        var noRatings = new List<string>();
        var modHeaders = string.Join(",", points.Select(x => RatingAttribute.GetCsvHeaderValue(x)));
        StringBuilder ratingBuilder = new($"username,{modHeaders}");

        if (exportOptions.HasFlag(ExportOptions.IncludeStarRating))
        {
            var srHeaders = points.Where(x => x.Scoring == ScoringRatingAttribute.Score).Select(x => RatingAttribute.GetCsvHeaderValue(x, true));
            ratingBuilder.Append(',');
            ratingBuilder.Append(string.Join(",", srHeaders));
        }

        ratingBuilder.Append('\n');


        foreach (var (requestedUsername, player) in players)
        {
            if (!ratings.ContainsKey((player!.PlayerId, 0)))
            {
                noRatings.Add(requestedUsername);
                continue;
            }

            ratingBuilder.Append($"{player.ActiveUsername}");

            foreach (var point in points)
            {
                var rating = ratings.GetValueOrDefault((player.PlayerId, point.AttributeId));
                var ordinal = rating?.Ordinal ?? 0d;
                ratingBuilder.Append($",{ordinal:F0}");
            }

            if (exportOptions.HasFlag(ExportOptions.IncludeStarRating))
            {
                foreach (var point in points.Where(x => x.Scoring == ScoringRatingAttribute.Score))
                {
                    var rating = ratings.GetValueOrDefault((player.PlayerId, point.AttributeId));
                    var starRating = rating?.StarRating ?? 0;
                    ratingBuilder.Append($",{starRating:F3}");
                }
            }

            ratingBuilder.AppendLine();
        }

        var predictionBuilder = new StringBuilder();

        var ratingGroups = ratings.GroupBy(x => x.Key.RatingAttributeId, x => (x.Key.Item1, x.Value)).ToList();

        var files = new List<FileAttachment>
        {
            new(new MemoryStream(Encoding.UTF8.GetBytes(ratingBuilder.ToString())), "ratings.csv", "Player ratings")
        };

        var playersWithRatingCount = players.Count - noRatings.Count;

        if (ratingGroups.Count != 0 && playersWithRatingCount > 1)
        {
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

            files.Add(new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(predictionBuilder.ToString())), "predictions.txt"));
        }

        var missingFile = GetMissingFile(manualMissing, spreadsheetMissing, noRatings);
        if (missingFile is not null) files.Add(new FileAttachment(new MemoryStream(missingFile), "missing.txt"));

        var (predictionsMessage, pluralPlayers) = playersWithRatingCount > 1 ? (" and predictions", "players") : ("", "player");
        await FollowupWithFilesAsync(files,
            $"Ratings{predictionsMessage} for {playersWithRatingCount} {pluralPlayers}.");
    }


    private byte[]? GetMissingFile(List<string> manualMissing, List<string> spreadsheetMissing, List<string> noRatings)
    {
        if (manualMissing.Count == 0 && spreadsheetMissing.Count == 0 && noRatings.Count == 0) return null;

        var messageBuilder = new StringBuilder();

        void EnumerateUsernames(List<string> missing)
        {
            for (int i = 0; i < missing.Count; i++)
                messageBuilder.AppendLine($"{i + 1}: {missing[i]}");
        }

        if (manualMissing.Count != 0)
        {
            messageBuilder.AppendLine("Missing from manual input:");
            EnumerateUsernames(manualMissing);
            messageBuilder.AppendLine();
        }

        if (noRatings.Count != 0)
        {
            messageBuilder.AppendLine("Players with no ratings:");
            EnumerateUsernames(noRatings);
            messageBuilder.AppendLine();
        }

        if (spreadsheetMissing.Count != 0)
        {
            messageBuilder.AppendLine("Missing from spreadsheet:");
            EnumerateUsernames(spreadsheetMissing);
            messageBuilder.AppendLine();
        }

        return Encoding.UTF8.GetBytes(messageBuilder.ToString());
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
        [Summary(description: @"Username format regex. Use all-caps USERNAME placeholder to match username. e.g \[\d+\] USERNAME")]
        string? spreadsheetUsernameRegex = null,
        [Summary(description: "Include detailed skillset")]
        bool includeDetailedSkillsets = false,
        [Summary(description: "Include PP statistics")]
        bool includePp = false,
        [Summary(description: "Include Accuracy and Combo statistics")]
        bool includeAccuracyAndCombo = false,
        [Summary(description: "Exclude unranked players")]
        bool excludeUnrankedPlayers = false,
        [Summary(description: "Include ratings' Star Ratings")]
        bool includeStarRatings = false)

    {
        await Catch(async () =>
        {
            await DeferAsync();

            var flags = ExportOptions.None;
            if (includePp) flags |= ExportOptions.IncludePP;
            if (includeAccuracyAndCombo) flags |= ExportOptions.IncludeAccuracyAndCombo;
            if (includeDetailedSkillsets) flags |= ExportOptions.IncludeDetailedSkillsets;
            if (excludeUnrankedPlayers) flags |= ExportOptions.ExcludeUnrankedPlayers;
            if (includeStarRatings) flags |= ExportOptions.IncludeStarRating;

            Regex? usernameRegex = null;
            if (spreadsheetUsernameRegex is not null)
                usernameRegex = new Regex(spreadsheetUsernameRegex.Replace("USERNAME", @"(?'username'[a-zA-Z0-9_\-\[\] ]{2,20})"),
                    RegexOptions.IgnoreCase);
            await ExportRatingsImpl(usernames, spreadsheet, usernameRegex, flags);
        });
    }

    private static string? MatchUsername(string username, Regex regex)
    {
        var match = regex.Match(username);

        return !match.Success ? null : match.Groups["username"].Value;
    }
}