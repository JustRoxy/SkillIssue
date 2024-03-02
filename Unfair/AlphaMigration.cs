using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using EFCore.BulkExtensions;
using MathNet.Numerics.Statistics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkillIssue.Database;
using SkillIssue.Domain.TGML.Entities;
using SkillIssue.Domain.Unfair.Entities;
using Unfair.Strategies.Beatmaps;
using Unfair.Strategies.Ratings;

namespace Unfair;

public class AlphaMigration(DatabaseContext context, ILogger<AlphaMigration> logger)
{
    public async Task BasicSync(IServiceScopeFactory scopeFactory)
    {
        var matches = context.TgmlMatches
            .AsNoTracking()
            .Where(x => x.EndTime != null && x.MatchStatus != TgmlMatchStatus.Ongoing)
            .Where(x => !(context.CalculationErrors.Any(z => z.MatchId == x.MatchId) &&
                          !context.Scores.Any(z => z.MatchId == x.MatchId)))
            .OrderBy(x => x.MatchId)
#if DEBUG
            .Take(100)
#endif
            .Select(x => new TgmlMatch
            {
                MatchId = x.MatchId,
                Name = x.Name,
                CompressedJson = x.CompressedJson,
                StartTime = x.StartTime,
                EndTime = x.EndTime
            });

        var total = await matches.CountAsync();
#if !DEBUG
        var beatmapAttributes =
            await context.BeatmapPerformances
                .AsNoTracking()
                .ToDictionaryAsync(x => (x.BeatmapId, x.Mods));
        
        var beatmapLookup = new PrecachedBeatmapLookup(beatmapAttributes);
#else
        IBeatmapLookup? beatmapLookup = null;
#endif
        var ratingLookup = new CachedRatingRepository(hideSource: true);

        List<CalculationResult> calculationResults = [];
        SemaphoreSlim saveLock = new(1);
        // Dictionary<int, List<(bool isHeadOnHead, double spearman, double mae, double mse)>> errorCollection = [];

        var xx = 0;
        List<Task> savingTasks = [];
        await foreach (var match in matches.AsAsyncEnumerable())
        {
            xx++;
            logger.LogInformation("Processing: {Amount} / {Total}", xx, total);
            await using var scope = scopeFactory.CreateAsyncScope();
            var unfairContext = scope.ServiceProvider.GetRequiredService<UnfairContext>();
            var calculationResult = await unfairContext.CalculateMatch(match, beatmapLookup, ratingLookup);
            if (xx % 50_000 == 0)
            {
                savingTasks.Add(Save(calculationResults.ToList()));
                calculationResults.Clear();
            }

            calculationResults.Add(calculationResult);
        }

        savingTasks.Add(Save(calculationResults));
        await Task.WhenAll(savingTasks);

        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(30));
        await context.BulkInsertOrUpdateAsync(ratingLookup.Cache.Select(x => x.Value));

        /*

        foreach (var error in errorCollection)
        {
            var attrs = RatingAttribute.GetAttributesFromId(error.Key);
            var ra = RatingAttribute.GetAttribute(attrs.modification, attrs.skillset, attrs.scoring);

            void PlotFor(bool isHeadOnHead)
            {
                using var plt = new Plot();
                var prefix = isHeadOnHead ? "HeadOnHead" : "TeamVsTeam";
                plt.Title($"{prefix} {ra.Description}");

                var collection = error.Value.Where(x => x.Item1 == isHeadOnHead).ToList();

                var xs = Enumerable.Range(1, collection.Count).ToArray();
                var spearman = collection.Select(x => x.spearman).ToArray();
                var mae = collection.Select(x => x.mae).ToArray();
                var mse = collection.Select(x => x.mse).ToArray();

                var splot = plt.Add.Scatter(xs, spearman);
                splot.Label = "Spearman";

                var maePlot = plt.Add.Scatter(xs, mae);
                maePlot.Label = "MAE";

                var msePlot = plt.Add.Scatter(xs, mse);
                msePlot.Label = "MSE";
                plt.ShowLegend();

                plt.SavePng($"Graphs/{error.Key}-{isHeadOnHead}.png", 1920, 1080);
            }

            PlotFor(true);
            PlotFor(false);
        }
        */

        return;

        async Task Save(List<CalculationResult> calculationErrors)
        {
            await using var localScope = scopeFactory.CreateAsyncScope();
            var database = localScope.ServiceProvider.GetRequiredService<DatabaseContext>();
            database.Database.SetCommandTimeout(TimeSpan.FromMinutes(30));
            logger.LogInformation("Start saving");
            var ratingHistories = calculationErrors.Where(x => x.RatingHistories != null)
                .SelectMany(x => x.RatingHistories!);

            var playerHistories = calculationErrors.Where(x => x.PlayerHistories != null)
                .SelectMany(x => x.PlayerHistories!);
            var tournamentMatches = calculationErrors.Where(x => x.Match != null).Select(x => x.Match!);
            var errors = calculationErrors.Select(x => x.CalculationError).ToList();

            var scores = calculationErrors.Where(x => x.Match?.Scores != null)
                .SelectMany(x => x.Match!.Scores!);

            /*
            var predictionErrors = ratingHistories.GroupBy(x => x.RatingAttributeId)
                .OrderBy(x => x.Key)
                .Select(x => (x.Key, x.GroupBy(z => z.GameId)
                    .Select(z => z.Select(y => (y.Rank, y.PredictedRank))
                        .OrderBy(y => y.Rank).ToList())
                    .Select(z =>
                    {
                        var dataA = z.Select(y => (double)y.Rank).ToArray();
                        var dataB = z.Select(y => (double)y.PredictedRank).ToArray();
                        var spearman = Correlation.Spearman(dataA, dataB);
                        var mse = MathNet.Numerics.Distance.MSE(dataA, dataB);
                        var mae = MathNet.Numerics.Distance.MAE(dataA, dataB);

                        return (spearman, mse, mae, z.Count);
                    })));


            foreach (var error in predictionErrors)
            {
                var attrs = RatingAttribute.GetAttributesFromId(error.Key);
                if (attrs.scoring == ScoringRatingAttribute.PP) continue;

                var ra = RatingAttribute.GetAttribute(attrs.modification, attrs.skillset, attrs.scoring);
                var r = CalculateAndLogErrors(ra, error.Item2.ToList());
                if (r is null) continue;

                foreach (var x in r)
                {
                    if (!errorCollection.TryGetValue(error.Key, out var e))
                    {
                        e = [];
                        errorCollection[error.Key] = e;
                    }

                    e.Add(x);
                }
            }
            */

            logger.LogInformation("Saving {Amount} matches", errors.Count);

            await saveLock.WaitAsync();
            await using var transaction = await database.Database.BeginTransactionAsync();
            try
            {
                await database.BulkInsertOrUpdateAsync(tournamentMatches);
                await database.BulkInsertOrUpdateAsync(scores);
                await database.BulkInsertOrUpdateAsync(playerHistories);
                await database.BulkInsertOrUpdateAsync(ratingHistories);
                await database.BulkInsertOrUpdateAsync(errors);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            await transaction.CommitAsync();
            saveLock.Release();
        }
    }

    private (bool headToHead, double spearman, double mae, double mse)[]? CalculateAndLogErrors(
        RatingAttribute ra, List<(double spearman, double mae, double mse, int teamSize)> error)
    {
        double[] GetQuartiles(double[] values)
        {
            return [values.LowerQuartile(), values.Mean(), values.UpperQuartile()];
        }

        double[]? CalculateAndLog(Func<int, bool> predicate, string desc)
        {
            var p = error.Where(x => predicate(x.teamSize)).ToList();
            if (p.Count == 0) return null;

            var spearman = p
                .Select(x => x.spearman)
                .Where(x => !double.IsNaN(x))
                .ToArray();

            var mae = error.Where(x => predicate(x.teamSize))
                .Select(x => x.mae)
                .Where(x => !double.IsNaN(x))
                .ToArray();

            var mse = error.Where(x => predicate(x.teamSize))
                .Select(x => x.mse)
                .Where(x => !double.IsNaN(x))
                .ToArray();

            if (spearman.Length == 0 || mae.Length == 0 || mse.Length == 0) return null;

            var sq = GetQuartiles(spearman);
            var maeq = GetQuartiles(mae);
            var mseq = GetQuartiles(mse);
            logger.LogInformation(
                "Errors for {RatingAttribute} ({AdditionalDescription}):\n\tSpearman: {Spearman:F2}\n\tMAE: {MeanAbsoluteError:F2}\n\tMSE: {MeanSquareError:F2}",
                ra.Description, desc, sq, maeq, mseq);

            return [sq[1], maeq[1], mseq[1]];
        }

        var headOnHead = CalculateAndLog(x => x == 2, "HeadOnHead");
        var teamVsTeam = CalculateAndLog(x => x > 2, "TeamVsTeam");

        if (headOnHead is null && teamVsTeam is null) return default;
        if (headOnHead is null)
            return [(false, teamVsTeam![0], teamVsTeam[1], teamVsTeam[2])];
        if (teamVsTeam is null)
            return [(true, headOnHead[0], headOnHead[1], headOnHead[2])];

        return
        [
            (true, headOnHead[0], headOnHead[1], headOnHead[2]),
            (false, teamVsTeam[0], teamVsTeam[1], teamVsTeam[2])
        ];
    }

    public async Task MigrateAvatarUrlAndCountryCode()
    {
        List<Player> players = [];
        var xx = 0;
        await foreach (var match in context.TgmlMatches
                           .AsNoTracking()
                           .Where(x => x.CompressedJson != null)
                           .Select(x => x.CompressedJson)
                           .AsAsyncEnumerable())
        {
            xx++;
            logger.LogInformation("Processing: {Amount}", xx);
            var decompress = JsonSerializer.Deserialize<JsonObject>(await Decompress(match));
            var users = decompress["users"].AsArray().Select(x => new Player
                {
                    PlayerId = x["id"].Deserialize<int>(),
                    ActiveUsername = x["username"].Deserialize<string>(),
                    CountryCode = x["country_code"].Deserialize<string>(),
                    AvatarUrl = x["avatar_url"].Deserialize<string>()
                })
                .ToList();

            players.AddRange(users);
        }

        var updatedPlayers = players.GroupBy(x => x.PlayerId)
            .Select(x => x.Last())
            .ToList();

        logger.LogInformation("Saving....");
        await context.BulkInsertOrUpdateAsync(updatedPlayers);
    }

    public async Task CheckPlayers()
    {
        var count = 0;
        var matches = context.TgmlMatches
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .OrderBy(x => x.MatchId)
            .Where(x => x.MatchId > 106747470)
            .Where(x => x.CompressedJson != null)
            .Include(x => x.Players);

        var xx = 0;
        await foreach (var match in matches.AsAsyncEnumerable())
        {
            xx++;
            var players = match.Players.Select(x => x.PlayerId).ToHashSet();
            var events = JsonSerializer.Deserialize<JsonObject>(await Decompress(match.CompressedJson!))!["events"]!
                .AsArray();

            var eventPlayers = events.Select(x => x?["user_id"])
                .Where(x => x is not null)
                .Select(x => x.Deserialize<int>())
                .Where(x => x != 0)
                .Distinct()
                .ToList();

            var nonExisting = eventPlayers.Where(x => !players.Contains(x)).ToList();
            if (nonExisting.Count == 0) continue;

            count++;
            logger.LogError("Couldn't find in {Name} ({Id}) players: {Players}", match.Name, match.MatchId,
                nonExisting);

            if (xx % 1000 == 0) logger.LogInformation("Processed: {Amount}", xx);
        }

        logger.LogInformation("Bugged: {Amount} matches", count);
    }

    private async Task<byte[]> Decompress(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var output = new MemoryStream();
        await using (var brotli = new BrotliStream(input, CompressionMode.Decompress))
        {
            await brotli.CopyToAsync(output);
        }

        return output.ToArray();
    }
}