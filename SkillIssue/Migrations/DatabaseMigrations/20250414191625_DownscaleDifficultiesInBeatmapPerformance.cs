using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillIssue.Migrations.DatabaseMigrations
{
    /// <inheritdoc />
    public partial class DownscaleDifficultiesInBeatmapPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "overall_difficulty",
                table: "beatmap_performance",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<float>(
                name: "approach_rate",
                table: "beatmap_performance",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "overall_difficulty",
                table: "beatmap_performance",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "approach_rate",
                table: "beatmap_performance",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");
        }
    }
}
