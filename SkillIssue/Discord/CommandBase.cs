using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SkillIssue.Discord.Commands.RatingCommands;
using SkillIssue.Domain.Discord;
using SkillIssue.Domain.Unfair.Entities;
using SkillIssue.Domain.Unfair.Enums;
using TheGreatSpy.Services;

namespace SkillIssue.Discord;

public abstract class CommandBase<T> : InteractionModuleBase
{
    protected abstract ILogger<T> Logger { get; }

    protected async Task<Player?> HandlePlayerRequest(string username, PlayerService playerService)
    {
        var player = await playerService.GetPlayer(username);
        if (player is not null) return player;

        await FollowupAsync($"Player {username} does not exist, please check the username spelling");
        return null;
    }

    protected async Task Catch(Func<Task> action)
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
            Logger.LogError(e, "An exception happened at rating commands");

            await FollowupAsync(embed: BuildError(e));
            throw;
        }
    }

    private Embed BuildError(Exception e)
    {
        return new EmbedBuilder()
            .WithTitle("oops, an error occured")
            .WithThumbnailUrl("https://osu.ppy.sh/images/layout/avatar-guest@2x.png")
            .WithDescription("Share the screenshot of this to @justroxy please :)")
            .WithColor(Color.Red)
            .AddField("Exception", e.Message)
            .WithCurrentTimestamp()
            .Build();
    }

    protected (ModificationRatingAttribute modification, SkillsetRatingAttribute skillset, ScoringRatingAttribute
        scoring, bool starRating) GetSelectedAttributes(
            SocketMessageComponent component)
    {
        var menus = component.Message.Components
            .SelectMany(x => x.Components)
            .Where(x => x.Type == ComponentType.SelectMenu)
            .Cast<SelectMenuComponent>()
            .Select(x =>
            {
                var id = x.CustomId.Split("-");
                return new
                {
                    Type = id[2],
                    Selected = int.Parse(id[3])
                };
            })
            .ToList();

        var selectedMod = (ModificationRatingAttribute?)menus.FirstOrDefault(x => x.Type == "mod")?
            .Selected ?? ModificationRatingAttribute.AllMods;
        var selectedSkillset = (SkillsetRatingAttribute?)menus.FirstOrDefault(x => x.Type == "skill")?
            .Selected ?? SkillsetRatingAttribute.Overall;

        var score = menus.FirstOrDefault(x => x.Type == "score")?.Selected;
        if (score == 100) return (selectedMod, selectedSkillset, ScoringRatingAttribute.Score, true);
        var selectedScoring = (ScoringRatingAttribute?)score ?? ScoringRatingAttribute.Score;

        return (selectedMod, selectedSkillset, selectedScoring, false);
    }

    protected IEnumerable<SelectMenuBuilder> GenerateAttributeSelectMenus(RatingAttribute currentAttribute, string tag)
    {
        var modificationMenu = new SelectMenuBuilder()
            .WithMinValues(1).WithMaxValues(1)
            .WithPlaceholder("Modification")
            .WithCustomId($"{tag}-mod");

        foreach (var mod in Enum.GetValues<ModificationRatingAttribute>())
            modificationMenu.AddOption($"{RatingAttribute.DescriptionFormat(mod)}", $"{(int)mod}",
                isDefault: mod == currentAttribute.Modification);

        yield return modificationMenu;

        var skillsetMenu = new SelectMenuBuilder()
            .WithMinValues(1).WithMaxValues(1)
            .WithPlaceholder("Skillset")
            .WithCustomId($"{tag}-skill");

        foreach (var skillset in Enum.GetValues<SkillsetRatingAttribute>())
            if (RatingAttribute.UsableRatingAttribute(currentAttribute.Modification, skillset))
            {
                var label = RatingAttribute.DescriptionFormat(skillset);
                skillsetMenu.AddOption(label, $"{(int)skillset}",
                    isDefault: skillset == currentAttribute.Skillset);
            }

        yield return skillsetMenu;

        var scoringMenu = new SelectMenuBuilder()
            .WithMinValues(1).WithMaxValues(1)
            .WithPlaceholder("Scoring")
            .WithCustomId($"{tag}-score");

        foreach (var scoring in Enum.GetValues<ScoringRatingAttribute>())
            scoringMenu.AddOption($"{RatingAttribute.DescriptionFormat(scoring)}",
                $"{(int)scoring}", isDefault: scoring == currentAttribute.Scoring);

        yield return scoringMenu;
    }

    protected async Task<bool> CheckUserId(InteractionState interaction)
    {
        if (interaction.CreatorId == Context.User.Id) return true;

        await RespondAsync("Only author of the command can interact with it :3", ephemeral: true);
        return false;
    }

    protected async Task<bool> CheckUserId(ulong authorId)
    {
        if (authorId == Context.User.Id) return true;

        await RespondAsync("Only author of the command can interact with it :3", ephemeral: true);
        return false;
    }
}