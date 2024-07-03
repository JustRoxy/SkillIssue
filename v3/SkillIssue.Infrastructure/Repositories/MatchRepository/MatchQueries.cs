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
               insert into match(match_id, status, name, is_tournament, start_time, end_time, content) 
               values (@MatchId, @Status, @Name, @IsTournament, @StartTime, @EndTime, @Content)
               on conflict(match_id) do update set 
               end_time = excluded.end_time
               ,content = excluded.content
               """;
    }

    public static CommandDefinition FindMatchesInStatus(int status, int limit, bool preoritizeTournamentMatches,
        CancellationToken cancellationToken)
    {
        var builder = new SqlBuilder();

        var template = builder.AddTemplate(
            "select match_id, status, name, is_tournament, start_time, end_time, content from match /**where**//**orderby**/ limit @limit",
            new
            {
                limit
            });

        builder.Where("status = @status", new { status });
        builder.OrderBy(preoritizeTournamentMatches ? "is_tournament DESC, match_id" : "match_id");

        return new CommandDefinition(template.RawSql, template.Parameters, cancellationToken: cancellationToken);
    }
}