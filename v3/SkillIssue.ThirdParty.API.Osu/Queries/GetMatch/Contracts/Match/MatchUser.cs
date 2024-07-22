using System.Text.Json.Serialization;
using SkillIssue.Common;

namespace SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts.Match;

public class MatchUser : IMergable<IList<MatchUser>>
{
    [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; }

    [JsonPropertyName("country_code")] public string CountryCode { get; set; }

    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("is_active")] public bool IsActive { get; set; }

    [JsonPropertyName("is_bot")] public bool IsBot { get; set; }

    [JsonPropertyName("is_deleted")] public bool IsDeleted { get; set; }

    [JsonPropertyName("is_online")] public bool IsOnline { get; set; }

    [JsonPropertyName("is_supporter")] public bool IsSupporter { get; set; }

    [JsonPropertyName("last_visit")] public DateTimeOffset? LastVisit { get; set; }

    [JsonPropertyName("username")] public string Username { get; set; }

    public static IList<MatchUser> Merge(IList<MatchUser> before, IList<MatchUser> after)
    {
        return before.UnionBy(after, key => key.Id).ToList();
    }
}