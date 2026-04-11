// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using System.Globalization;
using System.Text.RegularExpressions;
using Discord.Interactions;
using SkillIssue.API.Commands.Compliance;
using SkillIssue.Authorization;

namespace SkillIssue.Discord.Commands.ComplianceCommands;

[Group("compliance", "Compliance commands for official tournament support")]
public class ComplianceCommands(ILogger<ComplianceCommands> logger, OneTimeStorage oneTimeStorage) : CommandBase<ComplianceCommands>
{
    private readonly HashSet<ulong> _tournamentCommitteeMembers =
    [
        193256266959814656, // me, not a member, but a developer, JustRoxy
        181817053596876800, // AlbionTheGreat
        140893290647126017, // ChillierPear
        185648818111512576, // D I O
        335784192644218880, // enri
        308235272041005067, // Lightin
        214404841689186304, // nik
        151712925852237824, // Nopekjk
        178061085855711232, // Polytetral
        134388250553876480, // Raphalge
        111228770081337344, // Riot
        250693235091963904, // shdewz
        138760254191173632, // Snowleopard
        82986435782643712, // ThePoon
        125419051290853376, // this1guy
        135913775126544384, // Yazzehh
        203303038243438592, // YonGin
    ];

    protected override ILogger<ComplianceCommands> Logger { get; } = logger;

    [SlashCommand("lookup-on-date", "Receives a list of ratings before or at the specific date")]
    public async Task LookupOnDate(
        [Summary(description: "Space separated user ids", name: "user-ids")]
        string userIdsInput,
        [Summary(description: "Optional timestamp in year-month-day format. Current UTC date if empty.", name: "timestamp")]
        string? timestampInput = null
    )
    {
        await Catch(async () =>
        {
            if (!await ValidateTCommMember()) return;

            await DeferAsync(ephemeral: true);

            DateTime timestamp;

            if (string.IsNullOrWhiteSpace(timestampInput))
            {
                timestamp = DateTime.UtcNow.Date;
            }
            else if (!DateTime.TryParseExact(timestampInput, "yyyy-M-d", null, DateTimeStyles.AssumeUniversal, out timestamp))
            {
                await FollowupAsync("Please, provide a date in year-month-day format", ephemeral: true);
                return;
            }

            var userIds = Regex.Split(userIdsInput, @"(\s|,)").Where(x => !string.IsNullOrWhiteSpace(x)).Select(int.Parse).ToArray();
            var token = GenerateOneTimeToken();
            oneTimeStorage.Set(token, new LookupRatingsOnTimestampRequest
            {
                UserIds = userIds,
                Timestamp = timestamp
            });

            await FollowupAsync(
                $"One-time download link for your request (Lookup <= {timestamp:yyyy-MM-dd} for {userIds.Length} players): https://skillissue.app/compliance/lookup/{token}",
                ephemeral: true);
        });
    }

    [SlashCommand("accepted-matches", "Receives a list of accepted matches at specific timestamp")]
    public async Task AcceptedMatches(
        [Summary(description: "Timestamp in year-month-day format", name: "timestamp")]
        string? timestampInput = null)
    {
        await ProcessMatchListingRequest(EnumerateMatchesOnTimestampRequest.AcceptanceStatus.Accepted, timestampInput);
    }

    [SlashCommand("rejected-matches", "Receives a list of rejected matches at specific timestamp with a reason")]
    public async Task RejectedMatches(
        [Summary(description: "Timestamp in year-month-day format", name: "timestamp")]
        string? timestampInput = null)
    {
        await ProcessMatchListingRequest(EnumerateMatchesOnTimestampRequest.AcceptanceStatus.Rejected, timestampInput);
    }

    private async Task ProcessMatchListingRequest(EnumerateMatchesOnTimestampRequest.AcceptanceStatus status, string? timestampInput)
    {
        await Catch(async () =>
        {
            if (!await ValidateTCommMember()) return;

            await DeferAsync(ephemeral: true);

            DateTime timestamp;

            if (timestampInput is null) timestamp = DateTime.UtcNow;
            else if (!DateTime.TryParseExact(timestampInput, "yyyy-M-d", null, DateTimeStyles.AssumeUniversal, out timestamp))
            {
                await FollowupAsync("Please, provide a date in year-month-day format", ephemeral: true);
                return;
            }

            var token = GenerateOneTimeToken();
            oneTimeStorage.Set(token, new EnumerateMatchesOnTimestampRequest
            {
                Timestamp = timestamp,
                Status = status
            });

            var lobbyStatusString = status switch
            {
                EnumerateMatchesOnTimestampRequest.AcceptanceStatus.Accepted => "Accepted",
                EnumerateMatchesOnTimestampRequest.AcceptanceStatus.Rejected => "Rejected",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };

            await FollowupAsync(
                $"One-time download link for your request ({lobbyStatusString} Lobbies <= {timestamp:yyyy-MM-dd}): https://skillissue.app/compliance/match_listing/{token}",
                ephemeral: true);
        });
    }

    private static string GenerateOneTimeToken()
    {
        return Guid.NewGuid().ToString("N");
    }

    private async Task<bool> ValidateTCommMember()
    {
        if (_tournamentCommitteeMembers.Contains(Context.User.Id)) return true;

        await RespondAsync(
            "For now, only Tournament Committee members have automated access to /compliance commands. If you are not a member of the Tournament Committee, please contact @justroxy to provide the data for you.",
            ephemeral: true);

        return false;
    }
}