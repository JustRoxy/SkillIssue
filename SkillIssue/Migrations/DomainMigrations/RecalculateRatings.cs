using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using SkillIssue.Domain.Migrations;
using SkillIssue.Domain.Migrations.Attributes;
using SkillIssue.Domain.TGML.Entities;
using Unfair;
using Unfair.Strategies.Beatmaps;
using Unfair.Strategies.Ratings;

namespace SkillIssue.Migrations.DomainMigrations;

[RequiresDescription]
public class RecalculateRatings(
    DatabaseContext context,
    ILogger<RecalculateRatings> logger,
    IServiceScopeFactory scopeFactory) : DomainMigration
{
    private SemaphoreSlim saveLock = new(1);
    public override string MigrationName => "RecalculateRatings";

    protected override async Task OnMigration()
    {
        logger.LogWarning("truncating database");
        await context.Database.ExecuteSqlRawAsync("truncate match, score, player_history, rating_history, calculation_error");

        var matches = context.TgmlMatches
            .AsNoTracking()
            .Where(x => x.EndTime != null && x.MatchStatus != TgmlMatchStatus.Ongoing)
            // .Where(x => !(context.CalculationErrors.Any(z => z.MatchId == x.MatchId) &&
            // !context.Scores.Any(z => z.MatchId == x.MatchId)))
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
            Progressed(new Progress
            {
                MigrationStage = "Recalculation",
                Processed = xx,
                Total = total
            });
        }

        savingTasks.Add(Save(calculationResults));
        await Task.WhenAll(savingTasks);

        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(30));
        await context.BulkInsertOrUpdateAsync(ratingLookup.Cache.Select(x => x.Value));
    }

    private async Task Save(List<CalculationResult> calculationErrors)
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