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


        await discord.LoginAsync(TokenType.Bot, config.Token);
        await discord.StartAsync();
        discord.ShardReady += async client =>
        {
            var interaction = new InteractionService(client, new InteractionServiceConfig
            {
                LogLevel = LogSeverity.Info,
                DefaultRunMode = RunMode.Async,
                ThrowOnError = true,
                AutoServiceScopes = true
            });

            using var globalScope = serviceProvider.CreateScope();
            await interaction.AddModulesAsync(Assembly.GetEntryAssembly(), globalScope.ServiceProvider);

            if (isProduction)
                await interaction.RegisterCommandsGloballyAsync();
            else await interaction.RegisterCommandsToGuildAsync(993402063532863498); //Secret test guild :)

            client.InteractionCreated += async i =>
            {
                var scope = serviceProvider.CreateScope();
                var ctx = new SocketInteractionContext(client, i);
                await interaction.ExecuteCommandAsync(ctx, scope.ServiceProvider);
            };
        };
    }
}