using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SkillIssue.Common.MediatR.Behaviours;

namespace SkillIssue.Common.MediatR;

public static class MediatRExtensions
{
    public static void RegisterMediatR(this IServiceCollection services)
    {
        services.AddMediatR(config => config
            .AddOpenBehavior(typeof(ExceptionHandlingBehaviour<,>))
            .AddOpenBehavior(typeof(MetricsBehaviour<,>))
            .RegisterServicesFromAssembly(Assembly.GetEntryAssembly()!)
        );
    }
}