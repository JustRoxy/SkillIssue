using System.Collections.Concurrent;
using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using SkillIssue.Database;
using SkillIssue.Domain.Migrations;
using Beatmap = osu.Game.Beatmaps.Beatmap;

namespace SkillIssue.Migrations.DomainMigrations;

public class MigrateBeatmapMetadata(DatabaseContext context) : DomainMigration
{
    public override string MigrationName => nameof(MigrateBeatmapMetadata);

    protected override async Task OnMigration()
    {
        var beatmaps = context.Beatmaps
            .AsNoTracking()
            .Where(x => x.CompressedBeatmap != null && (x.Artist == null || x.Name == null || x.Version == null));

        var count = await beatmaps.CountAsync();
        var index = 0;
        ConcurrentQueue<object> modifications = [];

        await Parallel.ForEachAsync(beatmaps, async (beatmap, token) =>
        {
            var newIndex = Interlocked.Increment(ref index);
            var content = await beatmap.Decompress();

            using var memoryStream = new MemoryStream(content);
            using var stream = new LineBufferedReader(memoryStream);
            var decodedBeatmap = Decoder.GetDecoder<Beatmap>(stream).Decode(stream);

            beatmap.CompressedBeatmap = [];
            modifications.Enqueue(new
            {
                beatmap.BeatmapId,
                Artist = decodedBeatmap.BeatmapInfo.Metadata.Artist.Clone(),
                Name = decodedBeatmap.BeatmapInfo.Metadata.Title.Clone(),
                Version = decodedBeatmap.BeatmapInfo.DifficultyName.Clone()
            });

            Progressed(new Progress
            {
                MigrationStage = "Updating beatmap metadata",
                Processed = newIndex,
                Total = count
            });
        });

        Progressed(new Progress
        {
            MigrationStage = "Saving updated beatmap metadata",
            Processed = 0,
            Total = 1
        });

        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();
        try
        {
            await connection.ExecuteAsync("""
                                          update beatmap
                                          set version = @Version,
                                              name    = @Name,
                                              artist  = @Artist
                                          where beatmap_id = @BeatmapId
                                          """, modifications);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}