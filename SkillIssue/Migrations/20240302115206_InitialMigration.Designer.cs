﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SkillIssue.Database;

#nullable disable

namespace SkillIssue.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240302115206_InitialMigration")]
    partial class InitialMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("SkillIssue.Database.FlowStatusTracker", b =>
                {
                    b.Property<int>("MatchId")
                        .HasColumnType("integer")
                        .HasColumnName("match_id");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.HasKey("MatchId")
                        .HasName("pk_flow_status");

                    b.ToTable("flow_status", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.Discord.InteractionState", b =>
                {
                    b.Property<decimal>("MessageId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("message_id");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("creation_time");

                    b.Property<decimal>("CreatorId")
                        .HasColumnType("numeric(20,0)")
                        .HasColumnName("creator_id");

                    b.Property<int?>("PlayerId")
                        .HasColumnType("integer")
                        .HasColumnName("player_id");

                    b.Property<string>("StatePayload")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("state_payload");

                    b.HasKey("MessageId")
                        .HasName("pk_interactions");

                    b.ToTable("interactions", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.PPC.Entities.Beatmap", b =>
                {
                    b.Property<int>("BeatmapId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("beatmap_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("BeatmapId"));

                    b.Property<string>("Artist")
                        .HasColumnType("text")
                        .HasColumnName("artist");

                    b.Property<byte[]>("CompressedBeatmap")
                        .HasColumnType("bytea")
                        .HasColumnName("compressed_beatmap");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.Property<string>("Version")
                        .HasColumnType("text")
                        .HasColumnName("version");

                    b.HasKey("BeatmapId")
                        .HasName("pk_beatmap");

                    b.ToTable("beatmap", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.PPC.Entities.BeatmapPerformance", b =>
                {
                    b.Property<int>("BeatmapId")
                        .HasColumnType("integer")
                        .HasColumnName("beatmap_id");

                    b.Property<int>("Mods")
                        .HasColumnType("integer")
                        .HasColumnName("mods");

                    b.Property<double>("AimDifficulty")
                        .HasColumnType("double precision")
                        .HasColumnName("aim_difficulty");

                    b.Property<double>("ApproachRate")
                        .HasColumnType("double precision")
                        .HasColumnName("approach_rate");

                    b.Property<double>("Bpm")
                        .HasColumnType("double precision")
                        .HasColumnName("bpm");

                    b.Property<float>("CircleSize")
                        .HasColumnType("real")
                        .HasColumnName("circle_size");

                    b.Property<double>("DrainRate")
                        .HasColumnType("double precision")
                        .HasColumnName("drain_rate");

                    b.Property<double>("FlashlightDifficulty")
                        .HasColumnType("double precision")
                        .HasColumnName("flashlight_difficulty");

                    b.Property<int>("HitCircleCount")
                        .HasColumnType("integer")
                        .HasColumnName("hit_circle_count");

                    b.Property<int>("MaxCombo")
                        .HasColumnType("integer")
                        .HasColumnName("max_combo");

                    b.Property<double>("OverallDifficulty")
                        .HasColumnType("double precision")
                        .HasColumnName("overall_difficulty");

                    b.Property<int>("SliderCount")
                        .HasColumnType("integer")
                        .HasColumnName("slider_count");

                    b.Property<double>("SliderFactor")
                        .HasColumnType("double precision")
                        .HasColumnName("slider_factor");

                    b.Property<double>("SpeedDifficulty")
                        .HasColumnType("double precision")
                        .HasColumnName("speed_difficulty");

                    b.Property<double>("SpeedNoteCount")
                        .HasColumnType("double precision")
                        .HasColumnName("speed_note_count");

                    b.Property<int>("SpinnerCount")
                        .HasColumnType("integer")
                        .HasColumnName("spinner_count");

                    b.Property<double>("StarRating")
                        .HasColumnType("double precision")
                        .HasColumnName("star_rating");

                    b.HasKey("BeatmapId", "Mods")
                        .HasName("pk_beatmap_performance");

                    b.ToTable("beatmap_performance", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.TGML.Entities.TgmlMatch", b =>
                {
                    b.Property<int>("MatchId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("match_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("MatchId"));

                    b.Property<byte[]>("CompressedJson")
                        .HasColumnType("bytea")
                        .HasColumnName("compressed_json");

                    b.Property<DateTime?>("EndTime")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("end_time");

                    b.Property<int>("MatchStatus")
                        .HasColumnType("integer")
                        .HasColumnName("match_status");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("start_time");

                    b.HasKey("MatchId")
                        .HasName("pk_tgml_match");

                    b.HasIndex("MatchStatus")
                        .HasDatabaseName("ix_tgml_match_match_status");

                    b.ToTable("tgml_match", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.TGML.Entities.TgmlPlayer", b =>
                {
                    b.Property<int>("PlayerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("player_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("PlayerId"));

                    b.Property<string>("CurrentUsername")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("current_username");

                    b.HasKey("PlayerId")
                        .HasName("pk_tgml_player");

                    b.ToTable("tgml_player", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.CalculationError", b =>
                {
                    b.Property<int>("MatchId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("match_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("MatchId"));

                    b.Property<string>("CalculationErrorLog")
                        .HasColumnType("text")
                        .HasColumnName("calculation_error_log");

                    b.Property<int>("Flags")
                        .HasColumnType("integer")
                        .HasColumnName("flags");

                    b.HasKey("MatchId")
                        .HasName("pk_calculation_error");

                    b.ToTable("calculation_error", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.Player", b =>
                {
                    b.Property<int>("PlayerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("player_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("PlayerId"));

                    b.Property<string>("ActiveUsername")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("active_username");

                    b.Property<string>("AvatarUrl")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("avatar_url");

                    b.Property<string>("CountryCode")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("country_code");

                    b.Property<int?>("CountryRank")
                        .HasColumnType("integer")
                        .HasColumnName("country_rank");

                    b.Property<int?>("Digit")
                        .HasColumnType("integer")
                        .HasColumnName("digit");

                    b.Property<int?>("GlobalRank")
                        .HasColumnType("integer")
                        .HasColumnName("global_rank");

                    b.Property<bool>("IsRestricted")
                        .HasColumnType("boolean")
                        .HasColumnName("is_restricted");

                    b.Property<DateTime>("LastUpdated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_updated");

                    b.Property<double?>("Pp")
                        .HasColumnType("double precision")
                        .HasColumnName("pp");

                    b.HasKey("PlayerId")
                        .HasName("pk_player");

                    b.HasIndex("CountryCode")
                        .HasDatabaseName("ix_player_country_code");

                    b.HasIndex("Digit")
                        .HasDatabaseName("ix_player_digit")
                        .HasFilter("Digit IS NOT NULL");

                    b.HasIndex("IsRestricted")
                        .HasDatabaseName("ix_player_is_restricted")
                        .HasFilter("is_restricted = false");

                    b.ToTable("player", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.PlayerHistory", b =>
                {
                    b.Property<int>("PlayerId")
                        .HasColumnType("integer")
                        .HasColumnName("player_id");

                    b.Property<int>("MatchId")
                        .HasColumnType("integer")
                        .HasColumnName("match_id");

                    b.Property<double>("MatchCost")
                        .HasColumnType("double precision")
                        .HasColumnName("match_cost");

                    b.HasKey("PlayerId", "MatchId")
                        .HasName("pk_player_history");

                    b.HasIndex("MatchId")
                        .HasDatabaseName("ix_player_history_match_id");

                    b.ToTable("player_history", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.Rating", b =>
                {
                    b.Property<int>("RatingAttributeId")
                        .HasColumnType("integer")
                        .HasColumnName("rating_attribute_id");

                    b.Property<int>("PlayerId")
                        .HasColumnType("integer")
                        .HasColumnName("player_id");

                    b.Property<int>("GamesPlayed")
                        .HasColumnType("integer")
                        .HasColumnName("games_played");

                    b.Property<double>("Mu")
                        .HasColumnType("double precision")
                        .HasColumnName("mu");

                    b.Property<double>("Ordinal")
                        .HasColumnType("double precision")
                        .HasColumnName("ordinal");

                    b.Property<List<double>>("PerformancePoints")
                        .IsRequired()
                        .HasColumnType("double precision[]")
                        .HasColumnName("performance_points");

                    b.Property<double>("Sigma")
                        .HasColumnType("double precision")
                        .HasColumnName("sigma");

                    b.Property<double>("StarRating")
                        .HasColumnType("double precision")
                        .HasColumnName("star_rating");

                    b.Property<List<double>>("StarRatings")
                        .IsRequired()
                        .HasColumnType("double precision[]")
                        .HasColumnName("star_ratings");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.Property<int>("TotalOpponentsAmount")
                        .HasColumnType("integer")
                        .HasColumnName("total_opponents_amount");

                    b.Property<int>("WinAmount")
                        .HasColumnType("integer")
                        .HasColumnName("win_amount");

                    b.Property<double>("Winrate")
                        .HasColumnType("double precision")
                        .HasColumnName("winrate");

                    b.HasKey("RatingAttributeId", "PlayerId")
                        .HasName("pk_rating");

                    b.HasIndex("PlayerId")
                        .HasDatabaseName("ix_rating_player_id");

                    b.HasIndex("RatingAttributeId", "Ordinal", "Status")
                        .IsDescending()
                        .HasDatabaseName("ix_rating_rating_attribute_id_ordinal_status");

                    b.HasIndex("RatingAttributeId", "StarRating", "Status")
                        .IsDescending()
                        .HasDatabaseName("ix_rating_rating_attribute_id_star_rating_status");

                    b.ToTable("rating", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.RatingAttribute", b =>
                {
                    b.Property<int>("AttributeId")
                        .HasColumnType("integer")
                        .HasColumnName("attribute_id");

                    b.Property<short>("Modification")
                        .HasColumnType("smallint")
                        .HasColumnName("modification");

                    b.Property<short>("Scoring")
                        .HasColumnType("smallint")
                        .HasColumnName("scoring");

                    b.Property<short>("Skillset")
                        .HasColumnType("smallint")
                        .HasColumnName("skillset");

                    b.HasKey("AttributeId")
                        .HasName("pk_rating_attribute");

                    b.ToTable("rating_attribute", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.RatingHistory", b =>
                {
                    b.Property<int>("PlayerId")
                        .HasColumnType("integer")
                        .HasColumnName("player_id");

                    b.Property<long>("GameId")
                        .HasColumnType("bigint")
                        .HasColumnName("game_id");

                    b.Property<int>("RatingAttributeId")
                        .HasColumnType("integer")
                        .HasColumnName("rating_attribute_id");

                    b.Property<int>("MatchId")
                        .HasColumnType("integer")
                        .HasColumnName("match_id");

                    b.Property<short>("NewOrdinal")
                        .HasColumnType("smallint")
                        .HasColumnName("new_ordinal");

                    b.Property<float>("NewStarRating")
                        .HasColumnType("real")
                        .HasColumnName("new_star_rating");

                    b.Property<short>("OldOrdinal")
                        .HasColumnType("smallint")
                        .HasColumnName("old_ordinal");

                    b.Property<float>("OldStarRating")
                        .HasColumnType("real")
                        .HasColumnName("old_star_rating");

                    b.Property<byte>("PredictedRank")
                        .HasColumnType("smallint")
                        .HasColumnName("predicted_rank");

                    b.Property<byte>("Rank")
                        .HasColumnType("smallint")
                        .HasColumnName("rank");

                    b.HasKey("PlayerId", "GameId", "RatingAttributeId")
                        .HasName("pk_rating_history");

                    b.HasIndex("RatingAttributeId")
                        .HasDatabaseName("ix_rating_history_rating_attribute_id");

                    b.HasIndex("PlayerId", "MatchId")
                        .HasDatabaseName("ix_rating_history_player_id_match_id");

                    b.HasIndex("MatchId", "GameId", "PlayerId")
                        .HasDatabaseName("ix_rating_history_match_id_game_id_player_id");

                    b.ToTable("rating_history", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.Score", b =>
                {
                    b.Property<int>("MatchId")
                        .HasColumnType("integer")
                        .HasColumnName("match_id");

                    b.Property<long>("GameId")
                        .HasColumnType("bigint")
                        .HasColumnName("game_id");

                    b.Property<int>("PlayerId")
                        .HasColumnType("integer")
                        .HasColumnName("player_id");

                    b.Property<double>("Accuracy")
                        .HasColumnType("double precision")
                        .HasColumnName("accuracy");

                    b.Property<int?>("BeatmapId")
                        .HasColumnType("integer")
                        .HasColumnName("beatmap_id");

                    b.Property<int>("Count100")
                        .HasColumnType("integer")
                        .HasColumnName("count100");

                    b.Property<int>("Count300")
                        .HasColumnType("integer")
                        .HasColumnName("count300");

                    b.Property<int>("Count50")
                        .HasColumnType("integer")
                        .HasColumnName("count50");

                    b.Property<int>("CountMiss")
                        .HasColumnType("integer")
                        .HasColumnName("count_miss");

                    b.Property<int>("LegacyMods")
                        .HasColumnType("integer")
                        .HasColumnName("legacy_mods");

                    b.Property<int>("MaxCombo")
                        .HasColumnType("integer")
                        .HasColumnName("max_combo");

                    b.Property<double?>("Pp")
                        .HasColumnType("double precision")
                        .HasColumnName("pp");

                    b.Property<byte>("ScoringType")
                        .HasColumnType("smallint")
                        .HasColumnName("scoring_type");

                    b.Property<int>("TeamSide")
                        .HasColumnType("integer")
                        .HasColumnName("team_side");

                    b.Property<int>("TotalScore")
                        .HasColumnType("integer")
                        .HasColumnName("total_score");

                    b.HasKey("MatchId", "GameId", "PlayerId")
                        .HasName("pk_score");

                    b.HasIndex("BeatmapId")
                        .HasDatabaseName("ix_score_beatmap_id");

                    b.HasIndex("PlayerId", "MatchId")
                        .IsDescending()
                        .HasDatabaseName("ix_score_player_id_match_id");

                    b.ToTable("score", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.TournamentMatch", b =>
                {
                    b.Property<int>("MatchId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("match_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("MatchId"));

                    b.Property<string>("Acronym")
                        .HasColumnType("text")
                        .HasColumnName("acronym");

                    b.Property<string>("BlueTeam")
                        .HasColumnType("text")
                        .HasColumnName("blue_team");

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("end_time");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("RedTeam")
                        .HasColumnType("text")
                        .HasColumnName("red_team");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("start_time");

                    b.HasKey("MatchId")
                        .HasName("pk_match");

                    b.ToTable("match", (string)null);
                });

            modelBuilder.Entity("match_player", b =>
                {
                    b.Property<int>("MatchesMatchId")
                        .HasColumnType("integer")
                        .HasColumnName("matches_match_id");

                    b.Property<int>("PlayersPlayerId")
                        .HasColumnType("integer")
                        .HasColumnName("players_player_id");

                    b.HasKey("MatchesMatchId", "PlayersPlayerId")
                        .HasName("pk_match_player");

                    b.HasIndex("PlayersPlayerId")
                        .HasDatabaseName("ix_match_player_players_player_id");

                    b.ToTable("match_player", (string)null);
                });

            modelBuilder.Entity("SkillIssue.Domain.PPC.Entities.BeatmapPerformance", b =>
                {
                    b.HasOne("SkillIssue.Domain.PPC.Entities.Beatmap", "Beatmap")
                        .WithMany("Performances")
                        .HasForeignKey("BeatmapId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_beatmap_performance_beatmap_beatmap_id");

                    b.Navigation("Beatmap");
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.Player", b =>
                {
                    b.OwnsMany("SkillIssue.Domain.Unfair.Entities.PlayerUsername", "Usernames", b1 =>
                        {
                            b1.Property<string>("NormalizedUsername")
                                .HasColumnType("text")
                                .HasColumnName("normalized_username");

                            b1.Property<int>("PlayerId")
                                .HasColumnType("integer")
                                .HasColumnName("player_id");

                            b1.HasKey("NormalizedUsername")
                                .HasName("pk_player_username");

                            b1.HasIndex("PlayerId")
                                .HasDatabaseName("ix_player_username_player_id");

                            b1.ToTable("player_username", (string)null);

                            b1.WithOwner("Player")
                                .HasForeignKey("PlayerId")
                                .HasConstraintName("fk_player_username_players_player_id");

                            b1.Navigation("Player");
                        });

                    b.Navigation("Usernames");
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.PlayerHistory", b =>
                {
                    b.HasOne("SkillIssue.Domain.Unfair.Entities.TournamentMatch", "Match")
                        .WithMany()
                        .HasForeignKey("MatchId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_player_history_match_match_id");

                    b.HasOne("SkillIssue.Domain.Unfair.Entities.Player", "Player")
                        .WithMany()
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_player_history_player_player_id");

                    b.Navigation("Match");

                    b.Navigation("Player");
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.Rating", b =>
                {
                    b.HasOne("SkillIssue.Domain.Unfair.Entities.Player", "Player")
                        .WithMany("Ratings")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_rating_players_player_id");

                    b.HasOne("SkillIssue.Domain.Unfair.Entities.RatingAttribute", "RatingAttribute")
                        .WithMany()
                        .HasForeignKey("RatingAttributeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_rating_rating_attribute_rating_attribute_id");

                    b.Navigation("Player");

                    b.Navigation("RatingAttribute");
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.RatingHistory", b =>
                {
                    b.HasOne("SkillIssue.Domain.Unfair.Entities.TournamentMatch", "Match")
                        .WithMany()
                        .HasForeignKey("MatchId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_rating_history_match_match_id");

                    b.HasOne("SkillIssue.Domain.Unfair.Entities.Player", null)
                        .WithMany()
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_rating_history_player_player_id");

                    b.HasOne("SkillIssue.Domain.Unfair.Entities.RatingAttribute", "RatingAttribute")
                        .WithMany()
                        .HasForeignKey("RatingAttributeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_rating_history_rating_attribute_rating_attribute_id");

                    b.HasOne("SkillIssue.Domain.Unfair.Entities.PlayerHistory", "PlayerHistory")
                        .WithMany()
                        .HasForeignKey("PlayerId", "MatchId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_rating_history_player_history_player_id_match_id");

                    b.HasOne("SkillIssue.Domain.Unfair.Entities.Score", "Score")
                        .WithMany()
                        .HasForeignKey("MatchId", "GameId", "PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_rating_history_score_match_id_game_id_player_id");

                    b.Navigation("Match");

                    b.Navigation("PlayerHistory");

                    b.Navigation("RatingAttribute");

                    b.Navigation("Score");
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.Score", b =>
                {
                    b.HasOne("SkillIssue.Domain.PPC.Entities.Beatmap", "Beatmap")
                        .WithMany()
                        .HasForeignKey("BeatmapId")
                        .HasConstraintName("fk_score_beatmap_beatmap_id");

                    b.HasOne("SkillIssue.Domain.Unfair.Entities.TournamentMatch", "Match")
                        .WithMany("Scores")
                        .HasForeignKey("MatchId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_score_match_match_id");

                    b.HasOne("SkillIssue.Domain.Unfair.Entities.Player", "Player")
                        .WithMany()
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_score_players_player_id");

                    b.Navigation("Beatmap");

                    b.Navigation("Match");

                    b.Navigation("Player");
                });

            modelBuilder.Entity("match_player", b =>
                {
                    b.HasOne("SkillIssue.Domain.TGML.Entities.TgmlMatch", null)
                        .WithMany()
                        .HasForeignKey("MatchesMatchId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_match_player_tgml_match_matches_match_id");

                    b.HasOne("SkillIssue.Domain.TGML.Entities.TgmlPlayer", null)
                        .WithMany()
                        .HasForeignKey("PlayersPlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_match_player_tgml_player_players_player_id");
                });

            modelBuilder.Entity("SkillIssue.Domain.PPC.Entities.Beatmap", b =>
                {
                    b.Navigation("Performances");
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.Player", b =>
                {
                    b.Navigation("Ratings");
                });

            modelBuilder.Entity("SkillIssue.Domain.Unfair.Entities.TournamentMatch", b =>
                {
                    b.Navigation("Scores");
                });
#pragma warning restore 612, 618
        }
    }
}
