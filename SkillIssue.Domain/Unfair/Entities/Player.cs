namespace SkillIssue.Domain.Unfair.Entities;

public class Player
{
    public int PlayerId { get; set; }
    public string ActiveUsername { get; set; } = null!;
    public string CountryCode { get; set; } = null!;
    public string AvatarUrl { get; set; } = null!;
    public bool IsRestricted { get; set; }

    public int? GlobalRank { get; set; } = null;
    public int? CountryRank { get; set; } = null;

    public int? Digit
    {
        get
        {
            if (GlobalRank is null) return null;

            if (GlobalRank <= 9) return 1;
            if (GlobalRank <= 99) return 2;
            if (GlobalRank <= 999) return 3;
            if (GlobalRank <= 9999) return 4;
            if (GlobalRank <= 99999) return 5;
            if (GlobalRank <= 999999) return 6;
            return 7;
        }
        set { }
    }

    public double? Pp { get; set; } = null;

    public DateTime LastUpdated { get; set; }
    public IList<Rating> Ratings { get; set; } = null!;
    public IList<PlayerUsername> Usernames { get; set; } = null!;

    public static string NormalizeUsername(string username)
    {
        return username.ToLower();
    }

    public string GetUrl()
    {
        return $"https://osu.ppy.sh/users/{PlayerId}";
    }
}

public class PlayerUsername
{
    public int PlayerId { get; set; }
    public string NormalizedUsername { get; set; } = null!;

    public Player Player { get; set; } = null!;
}