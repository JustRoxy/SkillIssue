using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillIssue.Migrations.DatabaseMigrations
{
    /// <inheritdoc />
    public partial class AddMuAndSigmaToHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "new_mu",
                table: "rating_history",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "new_sigma",
                table: "rating_history",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "old_mu",
                table: "rating_history",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "old_sigma",
                table: "rating_history",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "new_mu",
                table: "rating_history");

            migrationBuilder.DropColumn(
                name: "new_sigma",
                table: "rating_history");

            migrationBuilder.DropColumn(
                name: "old_mu",
                table: "rating_history");

            migrationBuilder.DropColumn(
                name: "old_sigma",
                table: "rating_history");
        }
    }
}
