#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace SkillIssue.Migrations.DatabaseMigrations;

/// <inheritdoc />
public partial class AddDomainMigrations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "domain_migrations",
            table => new
            {
                migration_name = table.Column<string>("text", nullable: false),
                start_time = table.Column<DateTime>("timestamp with time zone", nullable: false),
                end_time = table.Column<DateTime>("timestamp with time zone", nullable: false),
                is_completed = table.Column<bool>("boolean", nullable: false)
            },
            constraints: table => { table.PrimaryKey("pk_domain_migrations", x => x.migration_name); });

        migrationBuilder.CreateIndex(
            "ix_score_player_id_pp",
            "score",
            new[] { "player_id", "pp" },
            descending: new bool[0],
            filter: "Pp IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "domain_migrations");

        migrationBuilder.DropIndex(
            "ix_score_player_id_pp",
            "score");
    }
}