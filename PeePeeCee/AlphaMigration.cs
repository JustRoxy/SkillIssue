using System.Data;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Dapper;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PeePeeCee.Services;
using SkillIssue.Database;
using SkillIssue.Domain.PPC.Entities;

namespace PeePeeCee;

public class AlphaMigration(DatabaseContext context, ILogger<AlphaMigration> logger)
{
    public async Task AddArtistAndSongName()
    {
        var titleRegex = new Regex(@"Title:(?'title'\s?.*$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        var artistRegex = new Regex(@"Artist:(?'artist'\s?.*$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        var versionRegex = new Regex(@"Version:(?'version'\s?.*$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var xx = 0;
        foreach (var beatmap in context.Beatmaps.Where(x => x.CompressedBeatmap != null).OrderBy(x => x.BeatmapId)
                     .AsEnumerable())
        {
            if (xx % 100_000 == 0) logger.LogInformation("Processing: {Amount}", ++xx);
            using var inputStream = new MemoryStream(beatmap.CompressedBeatmap!);
            using var outputStream = new MemoryStream();

            await using (var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress))
            {
                await brotliStream.CopyToAsync(outputStream);
            }

            var content = Encoding.UTF8.GetString(outputStream.ToArray());

            var title = titleRegex.Match(content).Groups["title"].Value.Trim();
            var artist = artistRegex.Match(content).Groups["artist"].Value.Trim();
            beatmap.Artist = artist;
            beatmap.Name = title;
            beatmap.Version = versionRegex.Match(content).Groups["version"].Value.Trim();
        }

        await context.SaveChangesAsync();
    }

    public async Task MigrateFromTGML()
    {
        var processed = (await context.Beatmaps.Select(x => x.BeatmapId).ToListAsync()).ToHashSet();
        var compressedData = await context.TgmlMatches
            .AsNoTracking()
            .OrderBy(x => x.MatchId)
            .Where(x => x.CompressedJson != null)
            .Select(x => x.CompressedJson)
            .ToListAsync();

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();
        try
        {
            var xx = 0;
            foreach (var match in compressedData)
            {
                ++xx;
                using var inputStream = new MemoryStream(match);
                using var outputStream = new MemoryStream();

                await using (var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress))
                {
                    await brotliStream.CopyToAsync(outputStream);
                }

                var json = JsonSerializer.Deserialize<JsonObject>(outputStream.ToArray());
                var beatmapIds = json!["events"]!
                    .AsArray().Where(x => x?["game"]?["beatmap"] is not null)
                    .Select(x => x!["game"]!["beatmap"]!["id"].Deserialize<int>())
                    .Where(x => !processed.Contains(x))
                    .ToList();

                if (xx % 1000 == 0) logger.LogInformation("Processed {Amount}", xx);

                if (beatmapIds.Count == 0) continue;

                await connection.ExecuteAsync(
                    "INSERT INTO beatmap (beatmap_id, status, compressed_beatmap) VALUES (@BeatmapId, @Status, @CompressedBeatmap) ON CONFLICT (beatmap_id) DO NOTHING",
                    beatmapIds.Select(x => new
                    {
                        BeatmapId = x,
                        Status = 0,
                        CompressedBeatmap = (byte[]?)null
                    }));

                if (xx % 1000 == 0)
                {
                    await transaction.CommitAsync();
                    transaction = await connection.BeginTransactionAsync();
                }
            }
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        await transaction.CommitAsync();
    }

    public async Task FetchMissingBeatmaps(BeatmapLookup lookup)
    {
        var toFetch = await context.Beatmaps
            .AsNoTracking()
            .Where(x => x.Status == BeatmapStatus.NeedsUpdate)
            .OrderBy(x => x.BeatmapId)
            .ToListAsync();

        if (toFetch.Count == 0)
        {
            logger.LogInformation("No fetching required");
            return;
        }

        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();

        try
        {
            var xx = 0;
            foreach (var beatmap in toFetch)
            {
                ++xx;

                var beatmapData = await lookup.GetBeatmap(beatmap.BeatmapId);
                if (string.IsNullOrWhiteSpace(beatmapData))
                {
                    beatmap.CompressedBeatmap = null;
                    beatmap.Status = BeatmapStatus.NotFound;
                }
                else
                {
                    using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(beatmapData));
                    using var outputStream = new MemoryStream();
                    await using (var brotli = new BrotliStream(outputStream, CompressionLevel.SmallestSize))
                    {
                        await inputStream.CopyToAsync(brotli);
                    }

                    var content = outputStream.ToArray();
                    beatmap.CompressedBeatmap = content;
                    beatmap.Status = BeatmapStatus.Ok;
                }

                await connection.ExecuteAsync(
                    "UPDATE beatmap SET status = @Status, compressed_beatmap = @CompressedBeatmap WHERE beatmap_id = @BeatmapId",
                    beatmap);

                if (xx % 100 == 0)
                {
                    logger.LogInformation("Processed {Amount} / {Total}", xx, toFetch.Count);
                    await transaction.CommitAsync();
                    transaction = await connection.BeginTransactionAsync();
                }
            }
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        await transaction.CommitAsync();
    }

    public async Task MigrateFromFilesystem(string path)
    {
        var processed =
            (await context.Beatmaps.Where(x => x.Status == BeatmapStatus.Ok)
                .Select(x => x.BeatmapId).ToListAsync())
            .ToHashSet();
        var files = Directory.GetFiles(path)
            .Select(x => new
            {
                Id = int.Parse(Path.GetFileNameWithoutExtension(x).Replace(".osu", "")),
                Path = x
            })
            .Where(x => !processed.Contains(x.Id))
            .ToList();

        var xx = 0;

        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();

        try
        {
            foreach (var file in files)
            {
                ++xx;

                await using var fileStream = File.OpenRead(file.Path);
                using var outputStream = new MemoryStream();
                await using (var brotli = new BrotliStream(outputStream, CompressionLevel.SmallestSize))
                {
                    await fileStream.CopyToAsync(brotli);
                }

                var content = outputStream.ToArray();

                await connection.ExecuteAsync(
                    "INSERT INTO beatmap (beatmap_id, status, compressed_beatmap) VALUES (@BeatmapId, @Status, @CompressedBeatmap) ON CONFLICT(beatmap_id) DO UPDATE SET status = 1, compressed_beatmap = excluded.compressed_beatmap",
                    new
                    {
                        BeatmapId = file.Id,
                        Status = 1,
                        CompressedBeatmap = content
                    });

                if (xx % 100 == 0)
                {
                    logger.LogInformation("Processed {Amount} / {Total}", xx, files.Count);
                    await transaction.CommitAsync();
                    transaction = await connection.BeginTransactionAsync();
                }
            }
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        await transaction.CommitAsync();
    }

    public async Task CalculateCurrentPerformances()
    {
        var toProcess = context.Beatmaps
            .AsNoTracking()
            .OrderBy(x => x.BeatmapId)
            .Where(x => x.Status == BeatmapStatus.Ok)
            .Where(x => x.CompressedBeatmap != null)
            .Where(x => !context.BeatmapPerformances.Any(z => z.BeatmapId == x.BeatmapId));

        var total = await toProcess.CountAsync();

        if (total == 0)
        {
            logger.LogInformation("No calculation required");
            return;
        }

        logger.LogInformation("Calculating {Amount} beatmaps", total);

        var xx = 0;

        var timestamp = Stopwatch.GetTimestamp();

        List<BeatmapPerformance> attributesBulk = [];
        foreach (var beatmap in toProcess.ToList())
        {
            xx++;
            logger.LogInformation("Processing: {Amount} / {Total}", xx, total);
            var file = await Decompress(beatmap.CompressedBeatmap!);
            var content = Encoding.UTF8.GetString(file);
            if (Regex.IsMatch(content, @"Mode:\s?") && !Regex.IsMatch(content, @"Mode:\s?0"))
            {
                logger.LogInformation("Skipping {BeatmapId} cause it's not an standard ruleset", beatmap.BeatmapId);
                continue;
            }

            try
            {
                var attributes = BeatmapProcessing.CalculateDifficultyAttributes(beatmap.BeatmapId, file);
                attributesBulk.AddRange(attributes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception at calculation");

                var incalculableBeatmap = await context.Beatmaps.FindAsync(beatmap.BeatmapId);
                incalculableBeatmap!.Status = BeatmapStatus.Incalculable;
                continue;
            }


            if (xx % 1000 == 0)
            {
                await context.BulkInsertAsync(attributesBulk);

                await context.SaveChangesAsync();
                attributesBulk = [];
                var elapsed = Stopwatch.GetElapsedTime(timestamp);
                context.ChangeTracker.Clear();
                logger.LogInformation("Processed {Amount} / {Total}, Estimated: {Time}", xx, total,
                    TimeSpan.FromSeconds((total - xx) / elapsed.TotalSeconds).ToString("g"));
                timestamp = Stopwatch.GetTimestamp();
            }
        }
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