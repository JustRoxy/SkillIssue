using FluentMigrator;

namespace SkillIssue.Infrastructure.Migrations;

[Migration(20240720)]
public class M20240720_AddMatchBeatmapTable : Migration
{
    public override void Up()
    {
        Execute.Sql("""
                    create table public.match_beatmap
                    (
                        beatmap_id integer not null
                            constraint match_beatmap_beatmap_id_to_beatmap_fk
                                references public.beatmap,
                        match_id   integer not null,
                        constraint match_beatmap_pk
                            primary key (beatmap_id, match_id)
                    );
                    """);
        
    }

    public override void Down()
    {
        Execute.Sql("drop table if exists match_beatmap");
    }
}