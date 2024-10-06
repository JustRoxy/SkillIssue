using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SkillIssue.Common.Database;

public static class PostgresExtensions
{
    public static IServiceCollection RegisterContext<T>(this IServiceCollection services, IConfiguration configuration, string schema) where T : DbContext
    {
        services.AddDbContextPool<T>(options =>
        {
            var builder = options.UseNpgsql(configuration.GetConnectionString("Postgres"),
                x => { x.MigrationsHistoryTable("__EFMigrationHistory", schema); });
            builder.EnableDetailedErrors();
            builder.UseSnakeCaseNamingConvention();
        });

        return services;
    }

    public static async Task RunMigrations<T>(this IServiceProvider serviceProvider) where T : DbContext
    {
        await using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
        await using var databaseContext = scope.ServiceProvider.GetRequiredService<T>();
        await databaseContext.Database.MigrateAsync();
    }
}