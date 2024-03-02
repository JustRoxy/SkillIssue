using System.Text;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using SkillIssue.Discord.Commands.RatingCommands;
using SkillIssue.Domain.Unfair.Entities;

namespace SkillIssue.Discord.Commands.DebugCommands;

[Group("debug", "Debug commands")]
public class DebugCommands(ILogger<DebugCommands> logger, DatabaseContext context) : InteractionModuleBase
{
    [SlashCommand("explain", "Explains match calculation")]
    public async Task Explain(string matchLink)
    {
        await Catch(async () =>
        {
            await DeferAsync();
            var id = int.Parse(matchLink.Split("/").Last());

            var match = await context.CalculationErrors.AsNoTracking().FirstOrDefaultAsync(x => x.MatchId == id);
            if (match is null)
            {
                await FollowupAsync("Match has not been calculated (yet?)");
                return;
            }

            if (match.Flags == CalculationErrorFlag.NoError || match.CalculationErrorLog is null)
            {
                await FollowupAsync("Match has been calculated successfully with no errors :)");
                return;
            }

            var logs = match.CalculationErrorLog.Split(";")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x =>
                {
                    var logSplit = x.Split(": ");
                    return new
                    {
                        Type = Enum.Parse<CalculationErrorFlag>(logSplit[0]),
                        Message = logSplit[1]
                    };
                });

            var sb = new StringBuilder();
            foreach (var log in logs) sb.AppendLine($"{log.Type} | {log.Message}");

            await FollowupAsync(Format.Code(sb.ToString()));
        });
    }

    private async Task Catch(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (UserInteractionException userInteractionException)
        {
            await FollowupAsync(userInteractionException.Message);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An exception happened at rating commands for user {UserId}", Context.User.Id);

            await FollowupAsync(embed: BuildError(e));
            throw;
        }
    }

    private Embed BuildError(Exception e)
    {
        return new EmbedBuilder()
            .WithTitle("oops, an error occured")
            .WithThumbnailUrl("https://osu.ppy.sh/images/layout/avatar-guest@2x.png")
            .WithDescription("Share the screenshot of this to me @justroxy please :)")
            .WithColor(Color.Red)
            .AddField("Exception", e.Message)
            .WithCurrentTimestamp()
            .Build();
    }
}