using System.Reflection;
using CommandLine;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using SkillIssue.Domain.Migrations;
using SkillIssue.Domain.Migrations.Attributes;
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

    public async Task RunDomainMigrations(DomainMigrationOptions options)
    {
        var migrationJournal = await _context
            .DomainMigrationJournal
            .ToListAsync();

        //TODO: create local migration DI scope 
        var migrations = ServiceProvider.GetServices<DomainMigration>().ToList();
        DomainMigrationProgress.MigrationsCount = migrations.Count;

        var forcedMigrations = ParseForcedMigrations(migrations, options);
        foreach (var migration in migrations)
        {
            var journal = migrationJournal.FirstOrDefault(x =>
                x.MigrationName == migration.MigrationName);

            var forcedMigration = forcedMigrations.FirstOrDefault(x => x.forcedMigration == migration);

            if (journal?.IsCompleted == true && forcedMigration == default)
            {
                _logger.LogInformation("Domain Migration {MigrationName} already applied, skipping...",
                    migration.MigrationName);
                continue;
            }

            if (forcedMigration != default)
            {
                _logger.LogWarning("Forcing migration: {MigrationName} with description {Description}",
                    migration.MigrationName, forcedMigration.description);
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
            DomainMigrationProgress.CurrentDescription = forcedMigration != default
                ? forcedMigration.description
                : null;

            await migration.Migrate();
            journal.EndTime = DateTime.UtcNow;
            journal.IsCompleted = true;
            await _context.SaveChangesAsync();
        }

        DomainMigrationProgress.CurrentMigration = null;
    }

    private List<(DomainMigration forcedMigration, string? description)> ParseForcedMigrations(
        List<DomainMigration> registeredMigartions,
        DomainMigrationOptions options)
    {
        if (options.Forced is null) return [];
        List<(DomainMigration forcedMigration, string? description)> forcedMigrations = [];
        foreach (var forcedMigration in options.Forced)
        {
            var nameDescriptionArray = forcedMigration.Split(":");
            if (nameDescriptionArray.Length == 0)
            {
                _logger.LogCritical("Migration name is not provided. Usage: \"MigrationName:Description\"");
                throw new Exception("Migration name is not provided. Usage: \"MigrationName:Description\"");
            }

            var forcedMigrationName = nameDescriptionArray[0];
            var foundMigration = registeredMigartions.FirstOrDefault(x =>
                string.Equals(x.MigrationName, forcedMigrationName, StringComparison.InvariantCultureIgnoreCase));

            if (foundMigration is null)
            {
                _logger.LogCritical("Migration {Name} does not exist", forcedMigration);
                throw new Exception($"Migration {forcedMigration} does not exist");
            }

            var requiresDescription = foundMigration.GetType().GetCustomAttribute<RequiresDescriptionAttribute>();
            if (requiresDescription is null)
            {
                forcedMigrations.Add((foundMigration, null));
                continue;
            }

            if (nameDescriptionArray.Length == 1)
            {
                _logger.LogCritical("Migration {Name} requires description. Usage: \"MigrationName:Description\"",
                    forcedMigration);
                throw new Exception(
                    $"Migration {forcedMigration} requires description. Usage: \"MigrationName:Description\"");
            }

            forcedMigrations.Add((foundMigration, nameDescriptionArray[1]));
        }

        return forcedMigrations;
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

public class DomainMigrationOptions
{
    [Option('f', "force-migration", Required = false,
        HelpText =
            "Forces re-evaluation of the migration. Some migrations require description, the format is the following: \"MigrationName:Description\"")]
    public IEnumerable<string>? Forced { get; set; }
}

public static class DomainMigrationProgress
{
    public static int ProcessedItems;
    public static int TotalItems;
    public static DomainMigrationJournal? CurrentMigration { get; set; }
    public static string? CurrentDescription { get; set; }
    public static string? CurrentMigrationStage { get; set; }
    public static int MigrationsCount { get; set; }
}