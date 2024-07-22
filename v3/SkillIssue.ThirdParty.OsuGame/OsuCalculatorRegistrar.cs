using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SkillIssue.ThirdParty.OsuGame;

public static class OsuCalculatorRegistrar
{
    public static IServiceCollection RegisterOsuCalculator(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<IDifficultyCalculator, DifficultyCalculator>();

        return services;
    }
}