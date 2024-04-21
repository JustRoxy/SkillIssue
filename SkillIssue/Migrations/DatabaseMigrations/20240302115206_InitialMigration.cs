#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace SkillIssue.Migrations.DatabaseMigrations;

/// <inheritdoc />
public partial class InitialMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "beatmap",
            table => new
            {
                beatmap_id = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                artist = table.Column<string>("text", nullable: true),
                name = table.Column<string>("text", nullable: true),
                version = table.Column<string>("text", nullable: true),
                status = table.Column<int>("integer", nullable: false),
                compressed_beatmap = table.Column<byte[]>("bytea", nullable: true)
            },
            constraints: table => { table.PrimaryKey("pk_beatmap", x => x.beatmap_id); });

        migrationBuilder.CreateTable(
            "calculation_error",
            table => new
            {
                match_id = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                flags = table.Column<int>("integer", nullable: false),
                calculation_error_log = table.Column<string>("text", nullable: true)
            },
            constraints: table => { table.PrimaryKey("pk_calculation_error", x => x.match_id); });

        migrationBuilder.CreateTable(
            "flow_status",
            table => new
            {
                match_id = table.Column<int>("integer", nullable: false),
                status = table.Column<int>("integer", nullable: false)
            },
            constraints: table => { table.PrimaryKey("pk_flow_status", x => x.match_id); });

        migrationBuilder.CreateTable(
            "interactions",
            table => new
            {
                message_id = table.Column<decimal>("numeric(20,0)", nullable: false),
                creator_id = table.Column<decimal>("numeric(20,0)", nullable: false),
                player_id = table.Column<int>("integer", nullable: true),
                creation_time = table.Column<DateTime>("timestamp with time zone", nullable: false),
                state_payload = table.Column<string>("text", nullable: false)
            },
            constraints: table => { table.PrimaryKey("pk_interactions", x => x.message_id); });

        migrationBuilder.CreateTable(
            "match",
            table => new
            {
                match_id = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>("text", nullable: false),
                start_time = table.Column<DateTime>("timestamp with time zone", nullable: false),
                end_time = table.Column<DateTime>("timestamp with time zone", nullable: false),
                acronym = table.Column<string>("text", nullable: true),
                red_team = table.Column<string>("text", nullable: true),
                blue_team = table.Column<string>("text", nullable: true)
            },
            constraints: table => { table.PrimaryKey("pk_match", x => x.match_id); });

        migrationBuilder.CreateTable(
            "player",
            table => new
            {
                player_id = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                active_username = table.Column<string>("text", nullable: false),
                country_code = table.Column<string>("text", nullable: false),
                avatar_url = table.Column<string>("text", nullable: false),
                is_restricted = table.Column<bool>("boolean", nullable: false),
                global_rank = table.Column<int>("integer", nullable: true),
                country_rank = table.Column<int>("integer", nullable: true),
                digit = table.Column<int>("integer", nullable: true),
                pp = table.Column<double>("double precision", nullable: true),
                last_updated = table.Column<DateTime>("timestamp with time zone", nullable: false)
            },
            constraints: table => { table.PrimaryKey("pk_player", x => x.player_id); });

        migrationBuilder.CreateTable(
            "rating_attribute",
            table => new
            {
                attribute_id = table.Column<int>("integer", nullable: false),
                modification = table.Column<short>("smallint", nullable: false),
                skillset = table.Column<short>("smallint", nullable: false),
                scoring = table.Column<short>("smallint", nullable: false)
            },
            constraints: table => { table.PrimaryKey("pk_rating_attribute", x => x.attribute_id); });

        migrationBuilder.CreateTable(
            "tgml_match",
            table => new
            {
                match_id = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>("text", nullable: false),
                start_time = table.Column<DateTime>("timestamp with time zone", nullable: false),
                end_time = table.Column<DateTime>("timestamp with time zone", nullable: true),
                match_status = table.Column<int>("integer", nullable: false),
                compressed_json = table.Column<byte[]>("bytea", nullable: true)
            },
            constraints: table => { table.PrimaryKey("pk_tgml_match", x => x.match_id); });

        migrationBuilder.CreateTable(
            "tgml_player",
            table => new
            {
                player_id = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                current_username = table.Column<string>("text", nullable: false)
            },
            constraints: table => { table.PrimaryKey("pk_tgml_player", x => x.player_id); });

        migrationBuilder.CreateTable(
            "beatmap_performance",
            table => new
            {
                beatmap_id = table.Column<int>("integer", nullable: false),
                mods = table.Column<int>("integer", nullable: false),
                star_rating = table.Column<double>("double precision", nullable: false),
                aim_difficulty = table.Column<double>("double precision", nullable: false),
                speed_difficulty = table.Column<double>("double precision", nullable: false),
                speed_note_count = table.Column<double>("double precision", nullable: false),
                flashlight_difficulty = table.Column<double>("double precision", nullable: false),
                slider_factor = table.Column<double>("double precision", nullable: false),
                approach_rate = table.Column<double>("double precision", nullable: false),
                overall_difficulty = table.Column<double>("double precision", nullable: false),
                drain_rate = table.Column<double>("double precision", nullable: false),
                hit_circle_count = table.Column<int>("integer", nullable: false),
                slider_count = table.Column<int>("integer", nullable: false),
                spinner_count = table.Column<int>("integer", nullable: false),
                max_combo = table.Column<int>("integer", nullable: false),
                bpm = table.Column<double>("double precision", nullable: false),
                circle_size = table.Column<float>("real", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_beatmap_performance", x => new { x.beatmap_id, x.mods });
                table.ForeignKey(
                    "fk_beatmap_performance_beatmap_beatmap_id",
                    x => x.beatmap_id,
                    "beatmap",
                    "beatmap_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "player_history",
            table => new
            {
                player_id = table.Column<int>("integer", nullable: false),
                match_id = table.Column<int>("integer", nullable: false),
                match_cost = table.Column<double>("double precision", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_player_history", x => new { x.player_id, x.match_id });
                table.ForeignKey(
                    "fk_player_history_match_match_id",
                    x => x.match_id,
                    "match",
                    "match_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "fk_player_history_player_player_id",
                    x => x.player_id,
                    "player",
                    "player_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "player_username",
            table => new
            {
                normalized_username = table.Column<string>("text", nullable: false),
                player_id = table.Column<int>("integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_player_username", x => x.normalized_username);
                table.ForeignKey(
                    "fk_player_username_players_player_id",
                    x => x.player_id,
                    "player",
                    "player_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "score",
            table => new
            {
                match_id = table.Column<int>("integer", nullable: false),
                game_id = table.Column<long>("bigint", nullable: false),
                player_id = table.Column<int>("integer", nullable: false),
                beatmap_id = table.Column<int>("integer", nullable: true),
                scoring_type = table.Column<byte>("smallint", nullable: false),
                team_side = table.Column<int>("integer", nullable: false),
                total_score = table.Column<int>("integer", nullable: false),
                accuracy = table.Column<double>("double precision", nullable: false),
                max_combo = table.Column<int>("integer", nullable: false),
                count300 = table.Column<int>("integer", nullable: false),
                count100 = table.Column<int>("integer", nullable: false),
                count50 = table.Column<int>("integer", nullable: false),
                count_miss = table.Column<int>("integer", nullable: false),
                legacy_mods = table.Column<int>("integer", nullable: false),
                pp = table.Column<double>("double precision", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_score", x => new { x.match_id, x.game_id, x.player_id });
                table.ForeignKey(
                    "fk_score_beatmap_beatmap_id",
                    x => x.beatmap_id,
                    "beatmap",
                    "beatmap_id");
                table.ForeignKey(
                    "fk_score_match_match_id",
                    x => x.match_id,
                    "match",
                    "match_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "fk_score_players_player_id",
                    x => x.player_id,
                    "player",
                    "player_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "rating",
            table => new
            {
                rating_attribute_id = table.Column<int>("integer", nullable: false),
                player_id = table.Column<int>("integer", nullable: false),
                mu = table.Column<double>("double precision", nullable: false),
                sigma = table.Column<double>("double precision", nullable: false),
                star_ratings = table.Column<List<double>>("double precision[]", nullable: false),
                performance_points = table.Column<List<double>>("double precision[]", nullable: false),
                status = table.Column<int>("integer", nullable: false),
                ordinal = table.Column<double>("double precision", nullable: false),
                star_rating = table.Column<double>("double precision", nullable: false),
                games_played = table.Column<int>("integer", nullable: false),
                win_amount = table.Column<int>("integer", nullable: false),
                total_opponents_amount = table.Column<int>("integer", nullable: false),
                winrate = table.Column<double>("double precision", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_rating", x => new { x.rating_attribute_id, x.player_id });
                table.ForeignKey(
                    "fk_rating_players_player_id",
                    x => x.player_id,
                    "player",
                    "player_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "fk_rating_rating_attribute_rating_attribute_id",
                    x => x.rating_attribute_id,
                    "rating_attribute",
                    "attribute_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "match_player",
            table => new
            {
                matches_match_id = table.Column<int>("integer", nullable: false),
                players_player_id = table.Column<int>("integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_match_player", x => new { x.matches_match_id, x.players_player_id });
                table.ForeignKey(
                    "fk_match_player_tgml_match_matches_match_id",
                    x => x.matches_match_id,
                    "tgml_match",
                    "match_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "fk_match_player_tgml_player_players_player_id",
                    x => x.players_player_id,
                    "tgml_player",
                    "player_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "rating_history",
            table => new
            {
                game_id = table.Column<long>("bigint", nullable: false),
                player_id = table.Column<int>("integer", nullable: false),
                rating_attribute_id = table.Column<int>("integer", nullable: false),
                match_id = table.Column<int>("integer", nullable: false),
                new_star_rating = table.Column<float>("real", nullable: false),
                old_star_rating = table.Column<float>("real", nullable: false),
                new_ordinal = table.Column<short>("smallint", nullable: false),
                old_ordinal = table.Column<short>("smallint", nullable: false),
                rank = table.Column<byte>("smallint", nullable: false),
                predicted_rank = table.Column<byte>("smallint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_rating_history", x => new { x.player_id, x.game_id, x.rating_attribute_id });
                table.ForeignKey(
                    "fk_rating_history_match_match_id",
                    x => x.match_id,
                    "match",
                    "match_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "fk_rating_history_player_history_player_id_match_id",
                    x => new { x.player_id, x.match_id },
                    "player_history",
                    new[] { "player_id", "match_id" },
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "fk_rating_history_player_player_id",
                    x => x.player_id,
                    "player",
                    "player_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "fk_rating_history_rating_attribute_rating_attribute_id",
                    x => x.rating_attribute_id,
                    "rating_attribute",
                    "attribute_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "fk_rating_history_score_match_id_game_id_player_id",
                    x => new { x.match_id, x.game_id, x.player_id },
                    "score",
                    new[] { "match_id", "game_id", "player_id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            "ix_match_player_players_player_id",
            "match_player",
            "players_player_id");

        migrationBuilder.CreateIndex(
            "ix_player_country_code",
            "player",
            "country_code");

        migrationBuilder.CreateIndex(
            "ix_player_digit",
            "player",
            "digit",
            filter: "Digit IS NOT NULL");

        migrationBuilder.CreateIndex(
            "ix_player_is_restricted",
            "player",
            "is_restricted",
            filter: "is_restricted = false");

        migrationBuilder.CreateIndex(
            "ix_player_history_match_id",
            "player_history",
            "match_id");

        migrationBuilder.CreateIndex(
            "ix_player_username_player_id",
            "player_username",
            "player_id");

        migrationBuilder.CreateIndex(
            "ix_rating_player_id",
            "rating",
            "player_id");

        migrationBuilder.CreateIndex(
            "ix_rating_rating_attribute_id_ordinal_status",
            "rating",
            new[] { "rating_attribute_id", "ordinal", "status" },
            descending: new bool[0]);

        migrationBuilder.CreateIndex(
            "ix_rating_rating_attribute_id_star_rating_status",
            "rating",
            new[] { "rating_attribute_id", "star_rating", "status" },
            descending: new bool[0]);

        migrationBuilder.CreateIndex(
            "ix_rating_history_match_id_game_id_player_id",
            "rating_history",
            new[] { "match_id", "game_id", "player_id" });

        migrationBuilder.CreateIndex(
            "ix_rating_history_player_id_match_id",
            "rating_history",
            new[] { "player_id", "match_id" });

        migrationBuilder.CreateIndex(
            "ix_rating_history_rating_attribute_id",
            "rating_history",
            "rating_attribute_id");

        migrationBuilder.CreateIndex(
            "ix_score_beatmap_id",
            "score",
            "beatmap_id");

        migrationBuilder.CreateIndex(
            "ix_score_player_id_match_id",
            "score",
            new[] { "player_id", "match_id" },
            descending: new bool[0]);

        migrationBuilder.CreateIndex(
            "ix_tgml_match_match_status",
            "tgml_match",
            "match_status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "beatmap_performance");

        migrationBuilder.DropTable(
            "calculation_error");

        migrationBuilder.DropTable(
            "flow_status");

        migrationBuilder.DropTable(
            "interactions");

        migrationBuilder.DropTable(
            "match_player");

        migrationBuilder.DropTable(
            "player_username");

        migrationBuilder.DropTable(
            "rating");

        migrationBuilder.DropTable(
            "rating_history");

        migrationBuilder.DropTable(
            "tgml_match");

        migrationBuilder.DropTable(
            "tgml_player");

        migrationBuilder.DropTable(
            "player_history");

        migrationBuilder.DropTable(
            "rating_attribute");

        migrationBuilder.DropTable(
            "score");

        migrationBuilder.DropTable(
            "beatmap");

        migrationBuilder.DropTable(
            "match");

        migrationBuilder.DropTable(
            "player");
    }
}