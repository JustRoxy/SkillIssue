using System.Collections.Concurrent;
using System.Data;
using System.IO.Compression;
using System.Text;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using osu.Game.Beatmaps;
using osu.Game.IO;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Utils;
using SkillIssue.Database;
using SkillIssue.Domain.PPC.Entities;
using Beatmap = SkillIssue.Domain.PPC.Entities.Beatmap;
using Decoder = osu.Game.Beatmaps.Formats.Decoder;

namespace PlayerPerformanceCalculator.Services;

public class BeatmapProcessing(DatabaseContext context, BeatmapLookup lookup, ILogger<BeatmapProcessing> logger)
{
    private static readonly ConcurrentDictionary<int, TaskCompletionSource<List<BeatmapPerformance>?>> Processing = [];
    private static readonly OsuRuleset OsuRuleset = new();

    private static readonly List<List<Mod>> ModCombinations =
        new OsuDifficultyCalculator(OsuRuleset.RulesetInfo, null)
            .CreateDifficultyAdjustmentModCombinations()
            .Select(x => ModUtils.FlattenMod(x).ToList())
            .ToList();

    private async Task<List<BeatmapPerformance>?> Process(int beatmapId, string? beatmapFile)
    {
        var beatmap = new Beatmap
        {
            BeatmapId = beatmapId,
            Status = BeatmapStatus.NeedsUpdate,
            CompressedBeatmap = null
        };

        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync();

        List<BeatmapPerformance>? attributes = null;

        if (string.IsNullOrWhiteSpace(beatmapFile))
        {
            logger.LogInformation("Beatmap {BeatmapId} has no content, marking it as {Mark}", beatmapId,
                BeatmapStatus.NotFound);
            beatmap.Status = BeatmapStatus.NotFound;
            await connection.ExecuteAsync(
                "INSERT INTO beatmap (beatmap_id, status, compressed_beatmap) VALUES (@BeatmapId, @Status, @CompressedBeatmap) ON CONFLICT(beatmap_id) DO UPDATE SET status = excluded.status",
                beatmap);

            return attributes;
        }

        var beatmapFileBytes = Encoding.UTF8.GetBytes(beatmapFile);
        using var inputStream = new MemoryStream(beatmapFileBytes);
        using var outputStream = new MemoryStream();

        await using (var brotli = new BrotliStream(outputStream, CompressionLevel.SmallestSize))
        {
            await inputStream.CopyToAsync(brotli);
        }

        var content = outputStream.ToArray();
        beatmap.CompressedBeatmap = content;
        beatmap.Status = BeatmapStatus.Ok;

        await using var transaction = await connection.BeginTransactionAsync();

        var workingBeatmap = GetBeatmap(beatmapFileBytes);
        beatmap.Artist = workingBeatmap.BeatmapInfo.Metadata.Artist;
        beatmap.Name = workingBeatmap.BeatmapInfo.Metadata.Title;
        beatmap.Version = workingBeatmap.BeatmapInfo.DifficultyName;

        if (workingBeatmap.BeatmapInfo.Ruleset.ShortName != "osu")
        {
            logger.LogInformation("Beatmap {BeatmapId} is not a standard beatmap, marking it as {Mark}", beatmapId,
                BeatmapStatus.Incalculable);
            beatmap.Status = BeatmapStatus.Incalculable;
        }
        else
        {
            try
            {
                attributes = CalculateDifficultyAttributes(beatmapId, workingBeatmap);

                logger.LogInformation("Successfully calculated beatmap {BeatmapId}", beatmapId);
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "Beatmap {BeatmapId} raised an exception on calculation, marking it as {Mark}",
                    beatmapId,
                    BeatmapStatus.Incalculable);
                beatmap.Status = BeatmapStatus.Incalculable;
            }
        }

        await connection.ExecuteAsync(
            @"INSERT INTO beatmap (beatmap_id, status, compressed_beatmap, artist, name, version) VALUES (@BeatmapId, @Status, @CompressedBeatmap, @Artist, @Name, @Version) ON CONFLICT (beatmap_id) DO UPDATE SET status = excluded.status, compressed_beatmap = excluded.compressed_beatmap, artist = excluded.artist, name = excluded.name, version = excluded.version",
            beatmap);

        if (attributes is not null)
            await connection.ExecuteAsync("""
                                          insert into beatmap_performance (beatmap_id, mods, star_rating, aim_difficulty, speed_difficulty, speed_note_count, flashlight_difficulty, slider_factor, approach_rate, overall_difficulty, drain_rate, hit_circle_count, slider_count, spinner_count, max_combo, bpm, circle_size, aim_difficult_slider_count, aim_difficult_strain_count, speed_difficult_strain_count)
                                          values (@BeatmapId, @Mods, @StarRating, @AimDifficulty, @SpeedDifficulty, @SpeedNoteCount, @FlashlightDifficulty, @SliderFactor, @ApproachRate, @OverallDifficulty, @DrainRate, @HitCircleCount, @SliderCount, @SpinnerCount, @MaxCombo, @Bpm, @CircleSize, @AimDifficultSliderCount, @AimDifficultStrainCount, @SpeedDifficultStrainCount) on conflict(beatmap_id, mods) do update set
                                          beatmap_id = excluded.beatmap_id,
                                          mods = excluded.mods,
                                          star_rating = excluded.star_rating,
                                          aim_difficulty = excluded.aim_difficulty,
                                          speed_difficulty = excluded.speed_difficulty,
                                          speed_note_count = excluded.speed_note_count,
                                          flashlight_difficulty = excluded.flashlight_difficulty,
                                          slider_factor = excluded.slider_factor,
                                          approach_rate = excluded.approach_rate,
                                          overall_difficulty = excluded.overall_difficulty,
                                          drain_rate = excluded.drain_rate,
                                          hit_circle_count = excluded.hit_circle_count,
                                          slider_count = excluded.slider_count,
                                          spinner_count = excluded.spinner_count,
                                          max_combo = excluded.max_combo,
                                          bpm = excluded.bpm,
                                          circle_size = excluded.circle_size,
                                          aim_difficult_slider_count = excluded.aim_difficult_slider_count,
                                          aim_difficult_strain_count = excluded.aim_difficult_strain_count,
                                          speed_difficult_strain_count = excluded.speed_difficult_strain_count
                                          """,
                attributes);

        await transaction.CommitAsync();

        return attributes;
    }

    public async Task<List<BeatmapPerformance>?> LookupAndProcess(int beatmapId)
    {
        logger.LogInformation("Begin {BeatmapId} processing", beatmapId);

        if (Processing.TryGetValue(beatmapId, out var existingTask))
        {
            logger.LogInformation("Beatmap {BeatmapId} already processing, waiting...", beatmapId);
            return await existingTask.Task;
        }

        var taskSource = new TaskCompletionSource<List<BeatmapPerformance>?>();
        Processing[beatmapId] = taskSource;

        var beatmapFile = await lookup.GetBeatmap(beatmapId);

        var attributes = await Process(beatmapId, beatmapFile);

        taskSource.TrySetResult(attributes);
        Processing.TryRemove(beatmapId, out _);

        return attributes;
    }

    public static IWorkingBeatmap GetBeatmap(byte[] beatmapBytes)
    {
        using var memoryStream = new MemoryStream(beatmapBytes);
        using var stream = new LineBufferedReader(memoryStream);
        var decoder = Decoder.GetDecoder<osu.Game.Beatmaps.Beatmap>(stream);
        var beatmap = decoder.Decode(stream);
        var workingBeatmap = new FlatWorkingBeatmap(beatmap);
        return workingBeatmap;
    }

    public static List<BeatmapPerformance> CalculateDifficultyAttributes(int beatmapId, IWorkingBeatmap beatmap)
    {
        var mostCommonBeatLength = beatmap.Beatmap.GetMostCommonBeatLength();

        var attributesList = ModCombinations
            .Select(mod => (mod,
                (OsuDifficultyAttributes)new OsuDifficultyCalculator(OsuRuleset.RulesetInfo, beatmap).Calculate(mod,
                    CancellationToken.None)))
            .Select(x =>
            {
                var attribute = x.Item2;
                var difficulty = beatmap.Beatmap.Difficulty;
                var rate = ModUtils.CalculateRateWithMods(x.mod);

                return new BeatmapPerformance
                {
                    BeatmapId = beatmapId,
                    Mods = (int)OsuRuleset.ConvertToLegacyMods(attribute.Mods),
                    StarRating = attribute.StarRating,
                    AimDifficulty = attribute.AimDifficulty,
                    SpeedDifficulty = attribute.SpeedDifficulty,
                    SpeedNoteCount = attribute.SpeedNoteCount,
                    FlashlightDifficulty = attribute.FlashlightDifficulty,
                    SliderFactor = attribute.SliderFactor,

                    //we should calculate AR OD and CS on the spot
                    ApproachRate = difficulty.ApproachRate,
                    OverallDifficulty = difficulty.OverallDifficulty,
                    DrainRate = attribute.DrainRate,
                    HitCircleCount = attribute.HitCircleCount,
                    SliderCount = attribute.SliderCount,
                    SpinnerCount = attribute.SpinnerCount,
                    MaxCombo = attribute.MaxCombo,
                    Bpm = GetBpm(mostCommonBeatLength, rate),
                    CircleSize = difficulty.CircleSize,
                    AimDifficultStrainCount = attribute.AimDifficultStrainCount,
                    AimDifficultSliderCount = attribute.AimDifficultSliderCount,
                    SpeedDifficultStrainCount = attribute.SpeedDifficultStrainCount
                };
            })
            .ToList();

        return attributesList;
    }

    private static double GetBpm(double commonBeatLength, double rate)
    {
        return 60000d / commonBeatLength * rate;
    }
}