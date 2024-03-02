using Discord;
using Discord.Webhook;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace SkillIssue;

public static class DiscordSinkExtensions
{
    public static LoggerConfiguration Discord(
        this LoggerSinkConfiguration loggerConfiguration,
        ulong webhookId,
        string webhookToken,
        IFormatProvider? formatProvider = null,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose)
    {
        return loggerConfiguration.Sink(
            new DiscordSink(formatProvider, webhookId, webhookToken, restrictedToMinimumLevel));
    }
}

public class DiscordSink(
    IFormatProvider? formatProvider,
    ulong webhookId,
    string webhookToken,
    LogEventLevel restrictedToMinimumLevel = LogEventLevel.Information)
    : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        SendMessage(logEvent);
    }

    private void SendMessage(LogEvent logEvent)
    {
        if (!ShouldLogMessage(restrictedToMinimumLevel, logEvent.Level))
            return;

        var embedBuilder = new EmbedBuilder();
        var webHook = new DiscordWebhookClient(webhookId, webhookToken);

        try
        {
            if (logEvent.Exception != null)
            {
                embedBuilder.Color = new Color(255, 0, 0);
                embedBuilder.WithTitle(":o: Exception");
                embedBuilder.AddField("Type:", $"```{logEvent.Exception.GetType().FullName}```");

                var message = FormatMessage(logEvent.Exception.Message, 1000);
                embedBuilder.AddField("Message:", message);

                if (logEvent.Exception.StackTrace != null)
                {
                    var stackTrace = FormatMessage(logEvent.Exception.StackTrace, 1000);
                    embedBuilder.AddField("StackTrace:", stackTrace);
                }

                webHook.SendMessageAsync(null, false, new[] { embedBuilder.Build() })
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                var message = logEvent.RenderMessage(formatProvider);

                message = FormatMessage(message, 240);

                SpecifyEmbedLevel(logEvent.Level, embedBuilder);

                embedBuilder.Description = message;

                webHook.SendMessageAsync(
                        null, false, new[] { embedBuilder.Build() })
                    .GetAwaiter()
                    .GetResult();
            }
        }
        catch (Exception ex)
        {
            webHook.SendMessageAsync(
                    $"ooo snap, {ex.Message}")
                .GetAwaiter()
                .GetResult();
        }
    }

    private static void SpecifyEmbedLevel(LogEventLevel level, EmbedBuilder embedBuilder)
    {
        switch (level)
        {
            case LogEventLevel.Verbose:
                embedBuilder.Title = ":loud_sound: Verbose";
                embedBuilder.Color = Color.LightGrey;
                break;
            case LogEventLevel.Debug:
                embedBuilder.Title = ":mag: Debug";
                embedBuilder.Color = Color.LightGrey;
                break;
            case LogEventLevel.Information:
                embedBuilder.Title = ":information_source: Information";
                embedBuilder.Color = new Color(0, 186, 255);
                break;
            case LogEventLevel.Warning:
                embedBuilder.Title = ":warning: Warning";
                embedBuilder.Color = new Color(255, 204, 0);
                break;
            case LogEventLevel.Error:
                embedBuilder.Title = ":x: Error";
                embedBuilder.Color = new Color(255, 0, 0);
                break;
            case LogEventLevel.Fatal:
                embedBuilder.Title = ":skull_crossbones: Fatal";
                embedBuilder.Color = Color.DarkRed;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    private static string FormatMessage(string message, int maxLength)
    {
        if (message.Length > maxLength)
            message = $"{message[..maxLength]} ...";

        if (!string.IsNullOrWhiteSpace(message))
            message = $"```{message}```";

        return message;
    }

    private static bool ShouldLogMessage(
        LogEventLevel minimumLogEventLevel,
        LogEventLevel messageLogEventLevel)
    {
        return (int)messageLogEventLevel >= (int)minimumLogEventLevel;
    }
}