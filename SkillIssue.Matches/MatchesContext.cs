using Microsoft.EntityFrameworkCore;
using SkillIssue.Matches.Models;

namespace SkillIssue.Matches;

public class MatchesContext(DbContextOptions<MatchesContext> options) : DbContext(options)
{
    public const string SCHEMA = "skillissue.matches";
    public DbSet<MatchFrame> MatchFrames { get; set; }
    public DbSet<Match> Matches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SCHEMA);

        modelBuilder.Entity<MatchFrame>(entity =>
        {
            entity.HasKey(e => new
            {
                e.MatchId,
                e.Cursor
            });

            entity.HasOne<Match>()
                .WithMany(x => x.Frames)
                .HasForeignKey(e => e.MatchId);
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(e => e.MatchId);
            entity.HasIndex(e => e.EndTime).HasFilter("end_time IS NULL");
        });
    }
}