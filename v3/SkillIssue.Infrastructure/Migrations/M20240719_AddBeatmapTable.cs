using FluentMigrator;

namespace SkillIssue.Infrastructure.Migrations;

[Migration(20240719)]
public class M20240719_AddBeatmapTable : Migration
{
    public override void Up()
    {
        Execute.Sql("""
                    create table public.beatmap
                    (
                        beatmap_id    integer     not null constraint PK_beatmap primary key,
                        status        smallint    not null default 0,
                        content       bytea,
                        last_update   timestamptz not null
                    );
                    """);
    }

    public override void Down()
    {
        Execute.Sql("""drop table if exists public.beatmap""");
    }
}