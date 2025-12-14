using MediatR;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Domain;
using SkillIssue.Domain.Discord;
using SkillIssue.Domain.Extensions;
using SkillIssue.Domain.Migrations;
using SkillIssue.Domain.PPC.Entities;
using SkillIssue.Domain.TGML.Entities;
using SkillIssue.Domain.Unfair.Entities;

namespace SkillIssue.Database;

public class DatabaseContext : DbContext
{
    private readonly IMediator? _mediator;

    public DatabaseContext(DbContextOptions<DatabaseContext> opts) : base(opts)
    {
    }

    public DatabaseContext(IMediator mediator)
    {
        _mediator = mediator;
    }

    public DatabaseContext(IMediator mediator, DbContextOptions<DatabaseContext> opts) : base(opts)
    {
        _mediator = mediator;
    }

    public DbSet<InteractionState> Interactions { get; set; } = null!;
    public DbSet<FlowStatusTracker> FlowStatus { get; init; } = null!;
    public DbSet<DomainMigrationJournal> DomainMigrationJournal { get; init; } = null!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (_mediator is null) return result;

        var changes = ChangeTracker.Entries<BaseEntity>()
            .Select(x => x.Entity)
            .Where(x => x.Events.Count != 0)
            .ToArray();

        await _mediator.PublishAndClear(changes);

        return result;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ModelPerformancePointsCalculator(modelBuilder);
        ModelTgml(modelBuilder);
        ModelUnfair(modelBuilder);

        var interactions = modelBuilder.Entity<InteractionState>().ToTable("interactions");
        interactions.HasKey(x => x.MessageId);
        interactions.Property(x => x.MessageId).ValueGeneratedNever();

        var flowStatus = modelBuilder.Entity<FlowStatusTracker>().ToTable("flow_status");
        flowStatus.HasKey(x => x.MatchId);
        flowStatus.Property(x => x.MatchId).ValueGeneratedNever();

        var domainMigrationJournal = modelBuilder.Entity<DomainMigrationJournal>().ToTable("domain_migrations");
        domainMigrationJournal.HasKey(x => x.MigrationName);
    }

    #region PPC

    private static void ModelPerformancePointsCalculator(ModelBuilder modelBuilder)
    {
        var beatmap = modelBuilder.Entity<Beatmap>().ToTable("beatmap");
        beatmap.HasKey(x => x.BeatmapId);

        var performance = modelBuilder.Entity<BeatmapPerformance>().ToTable("beatmap_performance");
        performance.HasKey(x => new
        {
            x.BeatmapId,
            x.Mods
        });

        performance.HasOne<Beatmap>(x => x.Beatmap)
            .WithMany(x => x.Performances)
            .HasForeignKey(x => x.BeatmapId);
    }

    public DbSet<Beatmap> Beatmaps { get; init; } = null!;

    public DbSet<BeatmapPerformance> BeatmapPerformances { get; init; } = null!;

    #endregion

    #region TheGreatMultiplayerLibrary

    private void ModelTgml(ModelBuilder modelBuilder)
    {
        var match = modelBuilder.Entity<TgmlMatch>().ToTable("tgml_match");

        match.HasIndex(x => x.MatchStatus);

        match.HasKey(x => x.MatchId);
        match.HasMany(x => x.Players)
            .WithMany(x => x.Matches)
            .UsingEntity("match_player");

        var player = modelBuilder.Entity<TgmlPlayer>().ToTable("tgml_player");
        player.HasKey(x => x.PlayerId);
    }

    public DbSet<TgmlMatch> TgmlMatches { get; init; } = null!;
    public DbSet<TgmlPlayer> TgmlPlayers { get; init; } = null!;

    #endregion

    #region Unfair

    private void ModelUnfair(ModelBuilder modelBuilder)
    {
        var match = modelBuilder.Entity<TournamentMatch>().ToTable("match");
        match.HasKey(x => x.MatchId);

        var score = modelBuilder.Entity<Score>().ToTable("score");
        score.HasKey(x => new
        {
            x.MatchId,
            x.GameId,
            x.PlayerId
        });
        score.HasOne<Player>(x => x.Player)
            .WithMany()
            .HasForeignKey(x => x.PlayerId);
        score.HasOne<TournamentMatch>(x => x.Match)
            .WithMany(x => x.Scores)
            .HasForeignKey(x => x.MatchId);
        score.HasOne(x => x.Beatmap)
            .WithMany()
            .HasForeignKey(x => x.BeatmapId);

        score.HasIndex(x => new
            {
                x.PlayerId,
                x.MatchId
            })
            .IsDescending();
        score.HasIndex(x => new
            {
                x.PlayerId,
                x.Pp
            })
            .IsDescending()
            .HasFilter("Pp IS NOT NULL");

        var ratingAttribute = modelBuilder.Entity<RatingAttribute>().ToTable("rating_attribute");
        ratingAttribute.HasKey(x => x.AttributeId);
        ratingAttribute.Property(x => x.AttributeId).ValueGeneratedNever();

        var rating = modelBuilder.Entity<Rating>().ToTable("rating");
        rating.HasKey(x => new
        {
            x.RatingAttributeId,
            x.PlayerId
        });
        rating.HasIndex(x => new
            {
                x.RatingAttributeId,
                x.Ordinal,
                x.Status
            })
            .IsDescending();

        rating.HasIndex(x => new
            {
                x.RatingAttributeId,
                x.StarRating,
                x.Status
            })
            .IsDescending();

        rating.HasOne(x => x.Player)
            .WithMany(x => x.Ratings)
            .HasForeignKey(x => x.PlayerId);

        rating.HasOne(x => x.RatingAttribute)
            .WithMany()
            .HasForeignKey(x => x.RatingAttributeId);

        var player = modelBuilder.Entity<Player>().ToTable("player");
        player.HasKey(x => x.PlayerId);
        player.OwnsMany(x => x.Usernames, x =>
        {
            x.ToTable("player_username");
            x.HasKey(z => new
            {
                z.NormalizedUsername
            });
            x.WithOwner(z => z.Player).HasForeignKey(z => z.PlayerId);
        });

        player.HasIndex(x => x.IsRestricted).HasFilter("is_restricted = false");
        player.HasIndex(x => x.CountryCode);
        player.HasIndex(x => x.Digit).HasFilter("Digit IS NOT NULL");
        player.HasIndex(x => x.GlobalRank);

        var playerHistory = modelBuilder.Entity<PlayerHistory>().ToTable("player_history");
        playerHistory.HasKey(x => new
        {
            x.PlayerId,
            x.MatchId
        });
        playerHistory.HasOne(x => x.Player)
            .WithMany()
            .HasForeignKey(x => x.PlayerId);

        playerHistory.HasOne(x => x.Match)
            .WithMany()
            .HasForeignKey(x => x.MatchId);

        var ratingHistory = modelBuilder.Entity<RatingHistory>().ToTable("rating_history");
        ratingHistory.HasKey(x => new
        {
            x.PlayerId,
            x.GameId,
            x.RatingAttributeId
        });
        ratingHistory.HasOne(x => x.RatingAttribute)
            .WithMany()
            .HasForeignKey(x => x.RatingAttributeId);
        ratingHistory.HasOne<Player>()
            .WithMany()
            .HasForeignKey(x => x.PlayerId);
        ratingHistory.HasOne<TournamentMatch>(x => x.Match)
            .WithMany()
            .HasForeignKey(x => x.MatchId);
        ratingHistory.HasOne<Score>(x => x.Score)
            .WithMany()
            .HasForeignKey(x => new
            {
                x.MatchId,
                x.GameId,
                x.PlayerId
            });

        ratingHistory.HasOne(x => x.PlayerHistory)
            .WithMany()
            .HasForeignKey(x => new
            {
                x.PlayerId,
                x.MatchId
            });

        var calculationError = modelBuilder.Entity<CalculationError>().ToTable("calculation_error");
        calculationError.HasKey(x => x.MatchId);
    }

    public DbSet<TournamentMatch> Matches { get; init; } = null!;
    public DbSet<Score> Scores { get; init; } = null!;
    public DbSet<RatingAttribute> RatingAttributes { get; init; } = null!;
    public DbSet<Rating> Ratings { get; init; } = null!;
    public DbSet<Player> Players { get; init; } = null!;

    public DbSet<PlayerHistory> PlayerHistories { get; init; } = null!;
    public DbSet<RatingHistory> RatingHistories { get; init; } = null!;

    public DbSet<CalculationError> CalculationErrors { get; set; }

    #endregion
}