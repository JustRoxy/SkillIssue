using FluentMigrator;

namespace SkillIssue.Infrastructure.Migrations;

[Migration(20240706)]
public class M20240706_CreateMatchTable : Migration
{
    public override void Up()
    {
        Execute.Sql("""
                    create table public.match
                    (
                        match_id      integer     not null constraint PK_match primary key,
                        status        smallint    not null default 0,
                        name          text        not null,
                        is_tournament bool        not null,
                        start_time    timestamptz not null,
                        end_time      timestamptz,
                        cursor        bigint
                    );
                    """);

        Execute.Sql("""
                    create table public.match_frame
                    (
                        match_id integer not null,
                        cursor   bigint  not null,
                        frame    bytea   not null,
                        constraint match_frame_pk
                            primary key (match_id, cursor)
                    );
                    """);
    }

    public override void Down()
    {
        Execute.Sql("drop table public.match");
        Execute.Sql("drop table public.match_frame");
    }
}