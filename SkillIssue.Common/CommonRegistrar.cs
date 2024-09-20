using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillIssue.Common.Broker;
using SkillIssue.Common.MediatR;

namespace SkillIssue.Common;

public static class CommonRegistrar
{
    public static void AddCommonServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRabbitMQ(configuration);
        services.RegisterMediatR();
    }
}