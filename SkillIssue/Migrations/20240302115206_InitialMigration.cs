using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillIssue.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "beatmap",
                columns: table => new
                {
                    beatmap_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    artist = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    compressed_beatmap = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beatmap", x => x.beatmap_id);
                });

            migrationBuilder.CreateTable(
                name: "calculation_error",
                columns: table => new
                {
                    match_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    flags = table.Column<int>(type: "integer", nullable: false),
                    calculation_error_log = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calculation_error", x => x.match_id);
                });

            migrationBuilder.CreateTable(
                name: "flow_status",
                columns: table => new
                {
                    match_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_flow_status", x => x.match_id);
                });

            migrationBuilder.CreateTable(
                name: "interactions",
                columns: table => new
                {
                    message_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    creator_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    player_id = table.Column<int>(type: "integer", nullable: true),
                    creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    state_payload = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interactions", x => x.message_id);
                });

            migrationBuilder.CreateTable(
                name: "match",
                columns: table => new
                {
                    match_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    acronym = table.Column<string>(type: "text", nullable: true),
                    red_team = table.Column<string>(type: "text", nullable: true),
                    blue_team = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_match", x => x.match_id);
                });

            migrationBuilder.CreateTable(
                name: "player",
                columns: table => new
                {
                    player_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    active_username = table.Column<string>(type: "text", nullable: false),
                    country_code = table.Column<string>(type: "text", nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: false),
                    is_restricted = table.Column<bool>(type: "boolean", nullable: false),
                    global_rank = table.Column<int>(type: "integer", nullable: true),
                    country_rank = table.Column<int>(type: "integer", nullable: true),
                    digit = table.Column<int>(type: "integer", nullable: true),
                    pp = table.Column<double>(type: "double precision", nullable: true),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player", x => x.player_id);
                });

            migrationBuilder.CreateTable(
                name: "rating_attribute",
                columns: table => new
                {
                    attribute_id = table.Column<int>(type: "integer", nullable: false),
                    modification = table.Column<short>(type: "smallint", nullable: false),
                    skillset = table.Column<short>(type: "smallint", nullable: false),
                    scoring = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rating_attribute", x => x.attribute_id);
                });

            migrationBuilder.CreateTable(
                name: "tgml_match",
                columns: table => new
                {
                    match_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    match_status = table.Column<int>(type: "integer", nullable: false),
                    compressed_json = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tgml_match", x => x.match_id);
                });

            migrationBuilder.CreateTable(
                name: "tgml_player",
                columns: table => new
                {
                    player_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    current_username = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tgml_player", x => x.player_id);
                });

            migrationBuilder.CreateTable(
                name: "beatmap_performance",
                columns: table => new
                {
                    beatmap_id = table.Column<int>(type: "integer", nullable: false),
                    mods = table.Column<int>(type: "integer", nullable: false),
                    star_rating = table.Column<double>(type: "double precision", nullable: false),
                    aim_difficulty = table.Column<double>(type: "double precision", nullable: false),
                    speed_difficulty = table.Column<double>(type: "double precision", nullable: false),
                    speed_note_count = table.Column<double>(type: "double precision", nullable: false),
                    flashlight_difficulty = table.Column<double>(type: "double precision", nullable: false),
                    slider_factor = table.Column<double>(type: "double precision", nullable: false),
                    approach_rate = table.Column<double>(type: "double precision", nullable: false),
                    overall_difficulty = table.Column<double>(type: "double precision", nullable: false),
                    drain_rate = table.Column<double>(type: "double precision", nullable: false),
                    hit_circle_count = table.Column<int>(type: "integer", nullable: false),
                    slider_count = table.Column<int>(type: "integer", nullable: false),
                    spinner_count = table.Column<int>(type: "integer", nullable: false),
                    max_combo = table.Column<int>(type: "integer", nullable: false),
                    bpm = table.Column<double>(type: "double precision", nullable: false),
                    circle_size = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beatmap_performance", x => new { x.beatmap_id, x.mods });
                    table.ForeignKey(
                        name: "fk_beatmap_performance_beatmap_beatmap_id",
                        column: x => x.beatmap_id,
                        principalTable: "beatmap",
                        principalColumn: "beatmap_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_history",
                columns: table => new
                {
                    player_id = table.Column<int>(type: "integer", nullable: false),
                    match_id = table.Column<int>(type: "integer", nullable: false),
                    match_cost = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_history", x => new { x.player_id, x.match_id });
                    table.ForeignKey(
                        name: "fk_player_history_match_match_id",
                        column: x => x.match_id,
                        principalTable: "match",
                        principalColumn: "match_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_player_history_player_player_id",
                        column: x => x.player_id,
                        principalTable: "player",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_username",
                columns: table => new
                {
                    normalized_username = table.Column<string>(type: "text", nullable: false),
                    player_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_username", x => x.normalized_username);
                    table.ForeignKey(
                        name: "fk_player_username_players_player_id",
                        column: x => x.player_id,
                        principalTable: "player",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "score",
                columns: table => new
                {
                    match_id = table.Column<int>(type: "integer", nullable: false),
                    game_id = table.Column<long>(type: "bigint", nullable: false),
                    player_id = table.Column<int>(type: "integer", nullable: false),
                    beatmap_id = table.Column<int>(type: "integer", nullable: true),
                    scoring_type = table.Column<byte>(type: "smallint", nullable: false),
                    team_side = table.Column<int>(type: "integer", nullable: false),
                    total_score = table.Column<int>(type: "integer", nullable: false),
                    accuracy = table.Column<double>(type: "double precision", nullable: false),
                    max_combo = table.Column<int>(type: "integer", nullable: false),
                    count300 = table.Column<int>(type: "integer", nullable: false),
                    count100 = table.Column<int>(type: "integer", nullable: false),
                    count50 = table.Column<int>(type: "integer", nullable: false),
                    count_miss = table.Column<int>(type: "integer", nullable: false),
                    legacy_mods = table.Column<int>(type: "integer", nullable: false),
                    pp = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_score", x => new { x.match_id, x.game_id, x.player_id });
                    table.ForeignKey(
                        name: "fk_score_beatmap_beatmap_id",
                        column: x => x.beatmap_id,
                        principalTable: "beatmap",
                        principalColumn: "beatmap_id");
                    table.ForeignKey(
                        name: "fk_score_match_match_id",
                        column: x => x.match_id,
                        principalTable: "match",
                        principalColumn: "match_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_score_players_player_id",
                        column: x => x.player_id,
                        principalTable: "player",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rating",
                columns: table => new
                {
                    rating_attribute_id = table.Column<int>(type: "integer", nullable: false),
                    player_id = table.Column<int>(type: "integer", nullable: false),
                    mu = table.Column<double>(type: "double precision", nullable: false),
                    sigma = table.Column<double>(type: "double precision", nullable: false),
                    star_ratings = table.Column<List<double>>(type: "double precision[]", nullable: false),
                    performance_points = table.Column<List<double>>(type: "double precision[]", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    ordinal = table.Column<double>(type: "double precision", nullable: false),
                    star_rating = table.Column<double>(type: "double precision", nullable: false),
                    games_played = table.Column<int>(type: "integer", nullable: false),
                    win_amount = table.Column<int>(type: "integer", nullable: false),
                    total_opponents_amount = table.Column<int>(type: "integer", nullable: false),
                    winrate = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rating", x => new { x.rating_attribute_id, x.player_id });
                    table.ForeignKey(
                        name: "fk_rating_players_player_id",
                        column: x => x.player_id,
                        principalTable: "player",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rating_rating_attribute_rating_attribute_id",
                        column: x => x.rating_attribute_id,
                        principalTable: "rating_attribute",
                        principalColumn: "attribute_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_player",
                columns: table => new
                {
                    matches_match_id = table.Column<int>(type: "integer", nullable: false),
                    players_player_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_match_player", x => new { x.matches_match_id, x.players_player_id });
                    table.ForeignKey(
                        name: "fk_match_player_tgml_match_matches_match_id",
                        column: x => x.matches_match_id,
                        principalTable: "tgml_match",
                        principalColumn: "match_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_match_player_tgml_player_players_player_id",
                        column: x => x.players_player_id,
                        principalTable: "tgml_player",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rating_history",
                columns: table => new
                {
                    game_id = table.Column<long>(type: "bigint", nullable: false),
                    player_id = table.Column<int>(type: "integer", nullable: false),
                    rating_attribute_id = table.Column<int>(type: "integer", nullable: false),
                    match_id = table.Column<int>(type: "integer", nullable: false),
                    new_star_rating = table.Column<float>(type: "real", nullable: false),
                    old_star_rating = table.Column<float>(type: "real", nullable: false),
                    new_ordinal = table.Column<short>(type: "smallint", nullable: false),
                    old_ordinal = table.Column<short>(type: "smallint", nullable: false),
                    rank = table.Column<byte>(type: "smallint", nullable: false),
                    predicted_rank = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rating_history", x => new { x.player_id, x.game_id, x.rating_attribute_id });
                    table.ForeignKey(
                        name: "fk_rating_history_match_match_id",
                        column: x => x.match_id,
                        principalTable: "match",
                        principalColumn: "match_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rating_history_player_history_player_id_match_id",
                        columns: x => new { x.player_id, x.match_id },
                        principalTable: "player_history",
                        principalColumns: new[] { "player_id", "match_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rating_history_player_player_id",
                        column: x => x.player_id,
                        principalTable: "player",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rating_history_rating_attribute_rating_attribute_id",
                        column: x => x.rating_attribute_id,
                        principalTable: "rating_attribute",
                        principalColumn: "attribute_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rating_history_score_match_id_game_id_player_id",
                        columns: x => new { x.match_id, x.game_id, x.player_id },
                        principalTable: "score",
                        principalColumns: new[] { "match_id", "game_id", "player_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_match_player_players_player_id",
                table: "match_player",
                column: "players_player_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_country_code",
                table: "player",
                column: "country_code");

            migrationBuilder.CreateIndex(
                name: "ix_player_digit",
                table: "player",
                column: "digit",
                filter: "Digit IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_player_is_restricted",
                table: "player",
                column: "is_restricted",
                filter: "is_restricted = false");

            migrationBuilder.CreateIndex(
                name: "ix_player_history_match_id",
                table: "player_history",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_username_player_id",
                table: "player_username",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_rating_player_id",
                table: "rating",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_rating_rating_attribute_id_ordinal_status",
                table: "rating",
                columns: new[] { "rating_attribute_id", "ordinal", "status" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_rating_rating_attribute_id_star_rating_status",
                table: "rating",
                columns: new[] { "rating_attribute_id", "star_rating", "status" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_rating_history_match_id_game_id_player_id",
                table: "rating_history",
                columns: new[] { "match_id", "game_id", "player_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rating_history_player_id_match_id",
                table: "rating_history",
                columns: new[] { "player_id", "match_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rating_history_rating_attribute_id",
                table: "rating_history",
                column: "rating_attribute_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_beatmap_id",
                table: "score",
                column: "beatmap_id");

            migrationBuilder.CreateIndex(
                name: "ix_score_player_id_match_id",
                table: "score",
                columns: new[] { "player_id", "match_id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_tgml_match_match_status",
                table: "tgml_match",
                column: "match_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "beatmap_performance");

            migrationBuilder.DropTable(
                name: "calculation_error");

            migrationBuilder.DropTable(
                name: "flow_status");

            migrationBuilder.DropTable(
                name: "interactions");

            migrationBuilder.DropTable(
                name: "match_player");

            migrationBuilder.DropTable(
                name: "player_username");

            migrationBuilder.DropTable(
                name: "rating");

            migrationBuilder.DropTable(
                name: "rating_history");

            migrationBuilder.DropTable(
                name: "tgml_match");

            migrationBuilder.DropTable(
                name: "tgml_player");

            migrationBuilder.DropTable(
                name: "player_history");

            migrationBuilder.DropTable(
                name: "rating_attribute");

            migrationBuilder.DropTable(
                name: "score");

            migrationBuilder.DropTable(
                name: "beatmap");

            migrationBuilder.DropTable(
                name: "match");

            migrationBuilder.DropTable(
                name: "player");
        }
    }
}
