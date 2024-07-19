using FluentMigrator;

namespace SkillIssue.Infrastructure.Migrations;

[Migration(20240722)]
public class M20240722_AddBeatmapDifficultyTable : Migration
{
    public override void Up()
    {
        Execute.Sql("""
                    create table beatmap_difficulty
                    (
                        beatmap_id integer not null,
                        mods integer not null,
                        
                        star_rating double precision not null, 
                        bpm integer not null, 
                        circle_size double precision not null, 
                        aim_difficulty double precision not null, 
                        speed_difficulty double precision not null,
                        speed_note_count double precision not null,
                        flashlight_difficulty double precision not null,
                        slider_factor double precision not null, 
                        approach_rate double precision not null,
                        overall_difficulty double precision not null,
                        drain_rate double precision not null, 
                        max_combo integer  not null,
                    constraint beatmap_difficulty_pk
                        primary key (beatmap_id, mods)
                    );
                    """);
    }

    public override void Down()
    {
        Execute.Sql("drop table if exists beatmap_difficulty");
    }
}