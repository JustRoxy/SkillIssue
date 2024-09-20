using EasyNetQ;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SkillIssue.Common.Broker;

public static class RabbitMQExtensions
{
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RabbitMQ");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("RabbitMQ connection string is not set");

        services.AddEasyNetQ(connectionString).UseSystemTextJsonV2();

        return services;
    }

    public static IServiceProvider RegisterConsumer<TMessage, TCommand>(this IServiceProvider serviceProvider,
        string subscriptionId,
        Func<TMessage, TCommand> mapping)
        where TMessage : class
        where TCommand : IRequest
    {
        var bus = serviceProvider.GetRequiredService<IBus>();
        bus.PubSub.Subscribe<TMessage>(subscriptionId, async message =>
        {
            await using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
            var mediatr = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediatr.Send(mapping(message));
        });
        
        return serviceProvider;
    }
}