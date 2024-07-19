using Dapper;
using SkillIssue.Infrastructure.Repositories.MatchFrameRepository.Contracts;

namespace SkillIssue.Infrastructure.Repositories.MatchFrameRepository;

public static class MatchFrameQueries
{
    public static CommandDefinition UpsertMatchFrame(MatchFrameRecord record, CancellationToken cancellationToken)
    {
        var builder = new SqlBuilder();

        var template =
            builder.AddTemplate(
                """
                insert into match_frame(match_id, cursor, frame) values (@MatchId, @Cursor, @Frame)
                on conflict(match_id, cursor) do update set 
                frame = excluded.frame
                """, record);

        return new CommandDefinition(template.RawSql, template.Parameters, cancellationToken: cancellationToken);
    }

    public static CommandDefinition GetMatchFramesWithBulk(IEnumerable<int> matchIds,
        CancellationToken cancellationToken)
    {
        var query = """
                    select match_id, cursor, frame from match_frame
                    where match_id = any(@MatchIds)
                    """;

        return new CommandDefinition(query, new
        {
            MatchIds = matchIds
        }, cancellationToken: cancellationToken);
    }
}