using Dapper;

namespace SkillIssue.Infrastructure.Repositories.MatchRepository;

public static class MatchQueries
{
    public static string FindLastMatchId()
    {
        return "select max(m.match_id) from match m";
    }

    public static string UpsertMatchesWithBulk()
    {
        return """
               insert into match(match_id, status, name, is_tournament, start_time, end_time, cursor) 
               values (@MatchId, @Status, @Name, @IsTournament, @StartTime, @EndTime, @Cursor)
               on conflict(match_id) do update set 
               end_time = excluded.end_time
               ,cursor = excluded.cursor
               """;
    }

    public static CommandDefinition FindMatchesInStatus(int status, int limit, bool preoritizeTournamentMatches,
        CancellationToken cancellationToken)
    {
        var builder = new SqlBuilder();

        var template = builder.AddTemplate(
            "select match_id, status, name, is_tournament, start_time, end_time, cursor from match /**where**//**orderby**/ limit @limit",
            new
            {
                limit
            });

        builder.Where("status = @status", new { status });
        builder.OrderBy(preoritizeTournamentMatches ? "is_tournament DESC, match_id" : "match_id");

        return new CommandDefinition(template.RawSql, template.Parameters, cancellationToken: cancellationToken);
    }

    public static CommandDefinition UpdateMatchCursor(int matchId, long cursor, DateTimeOffset lastTimestamp,
        CancellationToken cancellationToken)
    {
        var builder = new SqlBuilder();

        var template = builder.AddTemplate("""
                                           update match set 
                                           cursor = @Cursor
                                           ,last_updated_timestamp = @LastTimestamp
                                           /**where**/
                                           """,
            new
            {
                Cursor = cursor,
                LastTimestamp = lastTimestamp,
            });

        builder.Where("match_id = @matchId", new { matchId });

        return new CommandDefinition(template.RawSql, template.Parameters, cancellationToken: cancellationToken);
    }

    public static CommandDefinition MoveMatchToStatus(int matchId, int status,
        CancellationToken cancellationToken)
    {
        var builder = new SqlBuilder();

        var template = builder.AddTemplate("""
                                           update match set 
                                           status = @Status
                                           /**where**/
                                           """,
            new
            {
                Status = status
            });

        builder.Where("match_id = @matchId", new { matchId });

        return new CommandDefinition(template.RawSql, template.Parameters, cancellationToken: cancellationToken);
    }

    public static CommandDefinition MoveMatchToStatusWithEndTimeChange(int matchId, int completedStatus,
        DateTimeOffset endedAt,
        CancellationToken cancellationToken)
    {
        var builder = new SqlBuilder();

        var template = builder.AddTemplate("""
                                           update match set 
                                           end_time = @EndTime,
                                           status = @Status
                                           /**where**/
                                           """,
            new
            {
                EndTime = endedAt,
                Status = completedStatus
            });

        builder.Where("match_id = @matchId", new { matchId });

        return new CommandDefinition(template.RawSql, template.Parameters, cancellationToken: cancellationToken);
    }
}