using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillIssue.Infrastructure.Configuration;
using SkillIssue.Infrastructure.Repositories.MatchRepository;
using SkillIssue.Repository;

namespace SkillIssue.Infrastructure;

public static class InfrastructureRegistrar
{
    public static void RegisterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionFactory, PostgresConnectionFactory>();
        services.Configure<ConnectionStringConfiguration>(configuration.GetSection("ConnectionStrings"));
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        services.AddTransient<IMatchRepository, MatchRepository>();
    }
}