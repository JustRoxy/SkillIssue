using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillIssue.Infrastructure.Configuration;
using SkillIssue.Infrastructure.Migrations;
using SkillIssue.Infrastructure.Repositories.BeatmapRepository;
using SkillIssue.Infrastructure.Repositories.MatchFrameRepository;
using SkillIssue.Infrastructure.Repositories.MatchRepository;
using SkillIssue.Repository;

namespace SkillIssue.Infrastructure;

public static class InfrastructureRegistrar
{
    public static void RegisterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionStringSection = configuration.GetSection("ConnectionStrings");
        services.AddSingleton<IConnectionFactory, PostgresConnectionFactory>();
        services.Configure<ConnectionStringConfiguration>(connectionStringSection);
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        services.AddTransient<IMatchRepository, MatchRepository>();
        services.AddTransient<IMatchFrameRepository, MatchFrameRepository>();
        services.AddTransient<IBeatmapRepository, BeatmapRepository>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(runner =>
                runner.AddPostgres()
                    .WithGlobalConnectionString(connectionStringSection.Get<ConnectionStringConfiguration>()!.Postgres)
                    .ScanIn(typeof(M20240706_CreateMatchTable).Assembly).For.Migrations()
            );
    }
}