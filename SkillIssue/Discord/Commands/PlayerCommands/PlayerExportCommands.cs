using System.Text;
using Discord;
using Discord.Interactions;
using MathNet.Numerics;
using MathNet.Numerics.Integration;
using Microsoft.EntityFrameworkCore;
using SkillIssue.Database;
using SkillIssue.Discord.Commands.RatingCommands;
using SkillIssue.Domain.Extensions;
using SkillIssue.Domain.Unfair.Entities;

namespace SkillIssue.Discord.Commands.PlayerCommands;

[Group("players", "Bulk players commands (experimental)")]
public class PlayerExportCommands(ILogger<PlayerExportCommands> logger, DatabaseContext context) : CommandBase<PlayerExportCommands>
{
    [Flags]
    enum ExportOptions
    {
        None = 0,
        IncludeCountryCode = 1,
        IncludeGlobalRank = 2,
        IncludePP = 4,
    }

    public enum SortBy
    {
        Username,
        GlobalRank,
        PP,
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }

    record ExportParameters(string? CountryCode, int? BottomRank, int? TopRank, int? BottomPp, int? TopPp, bool? IsRanked);

    protected override ILogger<PlayerExportCommands> Logger { get; } = logger;

    [SlashCommand("export", "Export players' Bancho information in csv format. Should be used with /ratings export command")]
    public async Task ExportPlayers(
        SortBy sortBy,
        SortDirection sortDirection,
        [Summary(description: "Flag to include country code")]
        bool includeCountryCode = false,
        [Summary(description: "Flag to include global rank")]
        bool includeGlobalRank = false,
        [Summary(description: "Flag to include PP")]
        bool includePp = false,
        [Summary(description: "2-char country code")]
        string? filterCountryCode = null,
        [Summary(description: "Filter to only ranked players")]
        bool? filterIsRanked = null,
        [Summary(description: "Bottom rank range")]
        int? filterBottomRankRange = null,
        [Summary(description: "Top rank range")]
        int? filterTopRankRange = null,
        [Summary(description: "Bottom PP")] int? filterBottomPp = null,
        [Summary(description: "Top PP")] int? filterTopPp = null
    )
    {
        await Catch(async () =>
        {
            await DeferAsync();

            var flags = ExportOptions.None;
            if (includeCountryCode) flags |= ExportOptions.IncludeCountryCode;
            if (includeGlobalRank) flags |= ExportOptions.IncludeGlobalRank;
            if (includePp) flags |= ExportOptions.IncludePP;

            var exportParameters = new ExportParameters(filterCountryCode, filterBottomRankRange, filterTopRankRange, filterBottomPp, filterTopPp, filterIsRanked);
            await ExportPlayersImpl(sortBy, sortDirection, flags, exportParameters);
        });
    }

    private async Task ExportPlayersImpl(SortBy sort, SortDirection direction, ExportOptions flags, ExportParameters exportParameters)
    {
        var noFiltersSet = exportParameters.CountryCode is null &&
                           exportParameters.BottomRank is null &&
                           exportParameters.TopRank is null &&
                           exportParameters.BottomPp is null &&
                           exportParameters.TopPp is null;

        if (noFiltersSet) throw new UserInteractionException("One of the filters (excluding is-ranked) must be set");

        if (exportParameters.CountryCode is not null)
        {
            if (exportParameters.CountryCode.Length != 2)
                throw new UserInteractionException("Country code must be 2-char length. Example: CA, DE, etc.");
        }

        var mainQuery = context.Players
            .AsNoTracking()
            // sorry restricted players
            .Where(x => !x.IsRestricted)
            .Case(exportParameters.CountryCode is not null, players => players.Where(x => x.CountryCode == exportParameters.CountryCode!.ToUpper()))
            .Case(exportParameters.BottomRank is not null, players => players.Where(x => x.GlobalRank >= exportParameters.BottomRank))
            .Case(exportParameters.TopRank is not null, players => players.Where(x => x.GlobalRank <= exportParameters.TopRank))
            .Case(exportParameters.BottomPp is not null, players => players.Where(x => x.Pp >= exportParameters.BottomPp))
            .Case(exportParameters.TopPp is not null, players => players.Where(x => x.Pp <= exportParameters.TopPp))
            .Case(exportParameters.IsRanked == true, players => players.Where(x => x.Ratings.First(rating => rating.RatingAttributeId == 0).Status == RatingStatus.Ranked))
            .Case(sort == SortBy.GlobalRank,
                players => players
                    .Where(x => x.GlobalRank != null)
                    .If(direction == SortDirection.Ascending,
                        x => x.OrderBy(z => z.GlobalRank),
                        x => x.OrderByDescending(z => z.GlobalRank))
            )
            .Case(sort == SortBy.PP,
                players => players
                    .Where(x => x.Pp != null)
                    .If(direction == SortDirection.Ascending,
                        x => x.OrderBy(z => z.Pp),
                        x => x.OrderByDescending(z => z.Pp))
            )
            .Case(sort == SortBy.Username,
                players => players
                    .If(direction == SortDirection.Ascending,
                        x => x.OrderBy(z => z.ActiveUsername),
                        x => x.OrderByDescending(z => z.ActiveUsername))
            )
            .Select(x => new
            {
                x.GlobalRank,
                x.CountryCode,
                x.Pp,
                x.ActiveUsername
            });

        var count = await mainQuery.CountAsync();

        if (count > 20000) throw new UserInteractionException($"Provided export contains {count} players. Wow, that's a lot!");

        var players = await mainQuery.ToListAsync();
        List<string> headerList = ["username"];
        if (flags.HasFlag(ExportOptions.IncludeCountryCode)) headerList.Add("country_code");
        if (flags.HasFlag(ExportOptions.IncludeGlobalRank)) headerList.Add("global_rank");
        if (flags.HasFlag(ExportOptions.IncludePP)) headerList.Add("pp");

        var builder = new StringBuilder(string.Join(",", headerList) + "\n");

        foreach (var player in players)
        {
            builder.Append(player.ActiveUsername);

            if (flags.HasFlag(ExportOptions.IncludeCountryCode)) builder.Append($",{player.CountryCode}");
            if (flags.HasFlag(ExportOptions.IncludeGlobalRank)) builder.Append($",{player.GlobalRank}");
            if (flags.HasFlag(ExportOptions.IncludePP)) builder.Append($",{player.Pp?.Round(0):F0}");

            builder.Append("\n");
        }

        var fileAttachment = new FileAttachment(new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString())), "players.txt");

        await FollowupWithFileAsync(fileAttachment, $"Export for {players.Count} players");
    }
}