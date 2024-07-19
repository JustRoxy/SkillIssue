using FluentMigrator;

namespace SkillIssue.Infrastructure.Migrations;

[Migration(20240715)]
public class M20240715_AttLastUpdatedTimestamp : Migration
{
    public override void Up()
    {
        Execute.Sql("alter table match add column last_updated_timestamp timestamptz");
    }

    public override void Down()
    {
        Execute.Sql("alter table match drop column last_updated_timestamp");
    }
}