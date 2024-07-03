using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillIssue.Application.Services.IsTournamentMatch;

namespace SkillIssue.Application;

public static class ApplicationRegistrar
{
    public static void RegisterApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(config => config.RegisterServicesFromAssemblyContaining(typeof(ApplicationRegistrar)));
        services.AddTransient<IIsTournamentMatch, IsTournamentMatchValidator>();
    }
}