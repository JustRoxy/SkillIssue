using Microsoft.EntityFrameworkCore;
using SkillIssue.Beatmaps.Models;

namespace SkillIssue.Beatmaps;

public class BeatmapContext(DbContextOptions<BeatmapContext> options) : DbContext(options)
{
    public const string SCHEMA = "skillissue.beatmaps";

    public DbSet<Beatmap> Beatmaps { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SCHEMA);

        modelBuilder.Entity<Beatmap>(entity =>
        {
            entity.HasKey(e => e.BeatmapId);
            entity.Property(e => e.BeatmapId).IsRequired();
            entity.Property(e => e.Artist).IsRequired();
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Version).IsRequired();
        });
    }
}