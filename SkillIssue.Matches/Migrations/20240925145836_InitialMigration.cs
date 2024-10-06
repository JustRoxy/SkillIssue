using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillIssue.Matches.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "skillissue.matches");

            migrationBuilder.CreateTable(
                name: "matches",
                schema: "skillissue.matches",
                columns: table => new
                {
                    match_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    status = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_matches", x => x.match_id);
                });

            migrationBuilder.CreateTable(
                name: "match_frames",
                schema: "skillissue.matches",
                columns: table => new
                {
                    match_id = table.Column<int>(type: "integer", nullable: false),
                    cursor = table.Column<long>(type: "bigint", nullable: false),
                    data = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_match_frames", x => new { x.match_id, x.cursor });
                    table.ForeignKey(
                        name: "fk_match_frames_matches_match_id",
                        column: x => x.match_id,
                        principalSchema: "skillissue.matches",
                        principalTable: "matches",
                        principalColumn: "match_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "match_frames",
                schema: "skillissue.matches");

            migrationBuilder.DropTable(
                name: "matches",
                schema: "skillissue.matches");
        }
    }
}
