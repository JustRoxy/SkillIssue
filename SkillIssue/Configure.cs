using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace SkillIssue;

public class DiscordConfig
{
    public required string Token { get; set; }
    public required ulong UpdatesChannel { get; set; }
}

public static class Configure
{
    public static void AddDiscord(this IServiceCollection serviceCollection)
    {
        var discord = new DiscordShardedClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            GatewayIntents = GatewayIntents.AllUnprivileged ^ (GatewayIntents.GuildWebhooks |
                                                               GatewayIntents.GuildScheduledEvents |
                                                               GatewayIntents.GuildVoiceStates |
                                                               GatewayIntents.GuildInvites),
            UseInteractionSnowflakeDate = false
        });

        serviceCollection.AddSingleton<IDiscordClient>(discord);
    }

    private static Task Log(ILogger<DiscordShardedClient> logger, LogMessage message)
    {
        if (message.Exception is GatewayReconnectException) return Task.CompletedTask;

        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => throw new ArgumentOutOfRangeException(nameof(message))
        };

        logger.Log(level, message.Exception, "Discord.NET message: {DiscordMessage}", message.Message);

        return Task.CompletedTask;
    }

    public static async Task RunDiscord(this IServiceProvider serviceProvider, bool isProduction)
    {
        var config = serviceProvider.GetRequiredService<IOptions<DiscordConfig>>().Value;
        var discord = (DiscordShardedClient)serviceProvider.GetRequiredService<IDiscordClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<DiscordShardedClient>>();

        discord.Log += message => Log(logger, message);

        var interactionService = new InteractionService(discord, new InteractionServiceConfig
        {
            AutoServiceScopes = true
        });
        using (var interactionScope = serviceProvider.CreateScope())
        {
            await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), interactionScope.ServiceProvider);
        }

        discord.ShardReady += async client =>
        {
            logger.LogInformation("Shard {ShardId} is ready", client.ShardId);
            if (client.ShardId != 0) return;

            if (isProduction) await interactionService.RegisterCommandsGloballyAsync();
            else await interactionService.RegisterCommandsToGuildAsync(993402063532863498); //Secret test guild :)
        };

        discord.InteractionCreated += async i =>
        {
            var scope = serviceProvider.CreateScope();
            var ctx = new ShardedInteractionContext(discord, i);
            var result = await interactionService.ExecuteCommandAsync(ctx, scope.ServiceProvider);
            if (!result.IsSuccess) logger.LogError("InteractionCreated error: {Error}", result.ErrorReason);
        };

        await discord.LoginAsync(TokenType.Bot, config.Token);
        await discord.StartAsync();
    }
}