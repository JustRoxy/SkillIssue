using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using PlayerPerformanceCalculator.Services;
using SkillIssue.Database;
using SkillIssue.Domain.Migrations;
using SkillIssue.Domain.PPC.Entities;
using Beatmap = osu.Game.Beatmaps.Beatmap;

namespace SkillIssue.Migrations.DomainMigrations;

public class RecalculateBeatmapAttributes(IServiceProvider serviceProvider, ILogger<RecalculateBeatmapAttributes> logger) : DomainMigration
{
    public override string MigrationName => "RecalculateBeatmapAttributes";
    protected override async Task OnMigration()
    {
        logger.LogWarning("Clearing current beatmap attributes");
        await using var globalScope = serviceProvider.CreateAsyncScope();
        var queryContext = globalScope.ServiceProvider.GetRequiredService<DatabaseContext>();
        // await queryContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE beatmap_performance");

        var beatmaps = queryContext.Beatmaps
            .AsNoTracking()
            .Where(x => !x.Performances.Any() && x.CompressedBeatmap != null);

        var count = await beatmaps.CountAsync();
        var index = 0;

        var l_obj = new SemaphoreSlim(1);
        var batch = new List<List<BeatmapPerformance>>();
        await Parallel.ForEachAsync(beatmaps, async (beatmap, token) =>
        {
            var l_i = Interlocked.Increment(ref index);

            if (l_i % 50_000 == 0 || l_i == count)
            {
                await l_obj.WaitAsync(token);

                logger.LogInformation("Saving performance batch");
                await using var localScope = serviceProvider.CreateAsyncScope();
                var localContext = localScope.ServiceProvider.GetRequiredService<DatabaseContext>();
                var connection = localContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open) await connection.OpenAsync(token);

                await using var batchTransaction = await connection.BeginTransactionAsync(token);

                foreach (var performance in batch)
                {
                    await connection.ExecuteAsync("""
                                                  insert into beatmap_performance (beatmap_id, mods, star_rating, aim_difficulty, speed_difficulty, speed_note_count, flashlight_difficulty, slider_factor, approach_rate, overall_difficulty, drain_rate, hit_circle_count, slider_count, spinner_count, max_combo, bpm, circle_size, aim_difficult_slider_count, aim_difficult_strain_count, speed_difficult_strain_count)
                                                  values (@BeatmapId, @Mods, @StarRating, @AimDifficulty, @SpeedDifficulty, @SpeedNoteCount, @FlashlightDifficulty, @SliderFactor, @ApproachRate, @OverallDifficulty, @DrainRate, @HitCircleCount, @SliderCount, @SpinnerCount, @MaxCombo, @Bpm, @CircleSize, @AimDifficultSliderCount, @AimDifficultStrainCount, @SpeedDifficultStrainCount) 
                                                  """, performance, transaction: batchTransaction);
                }

                await batchTransaction.CommitAsync(token);

                batch.Clear();
                l_obj.Release();
            }

            var content = await beatmap.Decompress();

            try
            {
                using var memoryStream = new MemoryStream(content);
                using var stream = new LineBufferedReader(memoryStream);
                var decodedBeatmap = Decoder.GetDecoder<Beatmap>(stream).Decode(stream);
                var workingBeatmap = new FlatWorkingBeatmap(decodedBeatmap);

                var attributes = BeatmapProcessing.CalculateDifficultyAttributes(beatmap.BeatmapId, workingBeatmap);

                await l_obj.WaitAsync(token);
                batch.Add(attributes);
                l_obj.Release();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error on performance migration");
            }

            Progressed(new Progress
            {
                MigrationStage = "Updating beatmap attributes",
                Processed = index,
                Total = count
            });
        });
    }
}