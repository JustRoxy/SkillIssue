using System.Data;
using Dapper;
using SkillIssue.Domain;
using SkillIssue.Infrastructure.Repositories.BeatmapRepository.Contracts;

namespace SkillIssue.Infrastructure.Repositories.BeatmapRepository;

public class BeatmapQueries
{
    public static CommandDefinition UpdateBeatmap(BeatmapRecord record, CancellationToken cancellationToken)
    {
        var builder = new SqlBuilder();
        var template = builder.AddTemplate("""
                                           update beatmap /**set**/
                                           /**where**/
                                           """);
        builder.Set("beatmap_id = @BeatmapId, status = @Status, last_update = @LastUpdate", record);
        if (record.Content is not null)
        {
            builder.Set("content = @Content", record);
        }

        builder.Where("beatmap_id = @BeatmapId", record);

        return new CommandDefinition(template.RawSql, template.Parameters, cancellationToken: cancellationToken);
    }

    public static CommandDefinition InsertBeatmapsIfNotExistWithBulk(IEnumerable<BeatmapRecord> records,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var query = """
                    insert into beatmap(beatmap_id, status, content, last_update) values(@BeatmapId, @Status, @Content, @LastUpdate)
                    on conflict(beatmap_id) do nothing 
                    """;

        return new CommandDefinition(query, records, transaction, cancellationToken: cancellationToken);
    }

    public static CommandDefinition InsertMatchBeatmapRelationWithBulk(IEnumerable<MatchBeatmapRelationRecord> records,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var query = """
                    insert into match_beatmap(beatmap_id, match_id) values(@BeatmapId, @MatchId)
                    on conflict (beatmap_id, match_id) do nothing 
                    """;

        return new CommandDefinition(query, records, transaction, cancellationToken: cancellationToken);
    }

    public static CommandDefinition FindMatchBeatmapsWithStatus(int matchId, Beatmap.BeatmapStatus status,
        CancellationToken cancellationToken)
    {
        var builder = new SqlBuilder();

        var template = builder.AddTemplate("""
                                           select mb.beatmap_id from match_beatmap mb
                                           join public.beatmap b on b.beatmap_id = mb.beatmap_id
                                           /**where**/
                                           """);
        builder.Where("match_id = @matchId and status = @status", new
        {
            matchId,
            status
        });

        return new CommandDefinition(template.RawSql, template.Parameters, cancellationToken: cancellationToken);
    }

    public static CommandDefinition InsertDifficultiesWithBulk(IEnumerable<BeatmapDifficulty> difficulties,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var query = """
                    insert into beatmap_difficulty(beatmap_id, mods, star_rating, bpm, circle_size, aim_difficulty, speed_difficulty, speed_note_count, flashlight_difficulty, slider_factor, approach_rate, overall_difficulty, drain_rate, max_combo) values 
                    (@BeatmapId, @Mods, @StarRating, @Bpm, @CircleSize, @AimDifficulty, @SpeedDifficulty, @SpeedNoteCount, @FlashlightDifficulty, @SliderFactor, @ApproachRate, @OverallDifficulty, @DrainRate, @MaxCombo)
                    on conflict(beatmap_id, mods) do update set 
                    star_rating = excluded.star_rating,
                    bpm = excluded.bpm,
                    circle_size = excluded.circle_size, 
                    aim_difficulty = excluded.aim_difficulty, 
                    speed_difficulty = excluded.speed_difficulty, 
                    speed_note_count = excluded.speed_note_count, 
                    flashlight_difficulty = excluded.flashlight_difficulty, 
                    slider_factor = excluded.slider_factor, 
                    approach_rate = excluded.approach_rate, 
                    overall_difficulty = excluded.overall_difficulty, 
                    drain_rate = excluded.drain_rate, 
                    max_combo = excluded.max_combo
                    """;
        return new CommandDefinition(query, difficulties, transaction: transaction,
            cancellationToken: cancellationToken);
    }
}