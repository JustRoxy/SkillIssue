using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillIssue.Migrations.DatabaseMigrations
{
    /// <inheritdoc />
    public partial class AddDifficultAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "aim_difficult_slider_count",
                table: "beatmap_performance",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "aim_difficult_strain_count",
                table: "beatmap_performance",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "speed_difficult_strain_count",
                table: "beatmap_performance",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aim_difficult_slider_count",
                table: "beatmap_performance");

            migrationBuilder.DropColumn(
                name: "aim_difficult_strain_count",
                table: "beatmap_performance");

            migrationBuilder.DropColumn(
                name: "speed_difficult_strain_count",
                table: "beatmap_performance");
        }
    }
}
