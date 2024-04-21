using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using SkillIssue.Domain.Migrations;
using SkillIssue.Migrations.DomainMigrations;

namespace SkillIssue;

public class DomainMigrationRunner : IDisposable
{
    private readonly DatabaseContext _context;
    private readonly ILogger<DomainMigrationRunner> _logger;
    private readonly IServiceScope _scope;


    public DomainMigrationRunner(IServiceProvider serviceProvider)
    {
        _scope = serviceProvider.CreateScope();
        _logger = ServiceProvider.GetRequiredService<ILogger<DomainMigrationRunner>>();
        _context = ServiceProvider.GetRequiredService<DatabaseContext>();
    }

    private IServiceProvider ServiceProvider => _scope.ServiceProvider;

    public void Dispose()
    {
        _scope.Dispose();
        _context.Dispose();
    }

    public static IServiceCollection RegisterDomainMigrations(IServiceCollection collection)
    {
        collection.AddTransient<DomainMigrationRunner>();
        var migrations = typeof(MigrateBeatmapMetadata).Assembly.GetTypes()
            .Where(x => x.IsSubclassOf(typeof(DomainMigration)));

        foreach (var migration in migrations) collection.AddScoped(typeof(DomainMigration), migration);

        return collection;
    }

    public async Task RunDomainMigrations()
    {
        var migrationJournal = await _context
            .DomainMigrationJournal
            .ToListAsync();

        //TODO: create local migration DI scope 
        var migrations = ServiceProvider.GetServices<DomainMigration>().ToList();
        DomainMigrationProgress.MigrationsCount = migrations.Count;

        foreach (var migration in migrations)
        {
            var journal = migrationJournal.FirstOrDefault(x =>
                x.MigrationName == migration.MigrationName);

            if (journal?.IsCompleted == true)
            {
                _logger.LogInformation("Domain Migration {MigrationName} already applied, skipping...",
                    migration.MigrationName);
                continue;
            }

            if (journal == null)
            {
                journal = new DomainMigrationJournal
                {
                    MigrationName = migration.MigrationName,
                    IsCompleted = false
                };

                _context.DomainMigrationJournal.Add(journal);
            }

            journal.StartTime = DateTime.UtcNow;
            journal.IsCompleted = false;
            DomainMigrationProgress.CurrentMigration = journal;
            await _context.SaveChangesAsync();

            migration.OnProgess += MigrationOnProgress;
            _logger.LogInformation("Running migration {MigrationName}", migration.MigrationName);
            await migration.Migrate();

            journal.EndTime = DateTime.UtcNow;
            journal.IsCompleted = true;
            await _context.SaveChangesAsync();
        }

        DomainMigrationProgress.CurrentMigration = null;
    }

    private void MigrationOnProgress(DomainMigration.Progress obj)
    {
        _logger.LogDebug("Migration {MigrationName} ({MigrationStage}): {Processed} / {Total}",
            DomainMigrationProgress.CurrentMigration?.MigrationName,
            obj.MigrationStage,
            obj.Processed,
            obj.Total);
        DomainMigrationProgress.CurrentMigrationStage = obj.MigrationStage;
        DomainMigrationProgress.ProcessedItems = obj.Processed;
        DomainMigrationProgress.TotalItems = obj.Total;
    }
}

public static class DomainMigrationProgress
{
    public static int ProcessedItems;
    public static int TotalItems;
    public static DomainMigrationJournal? CurrentMigration { get; set; }
    public static string? CurrentMigrationStage { get; set; }
    public static int MigrationsCount { get; set; }
}