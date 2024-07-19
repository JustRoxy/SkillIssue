namespace SkillIssue.ThirdParty.Osu.Queries.GetMatch.Bugs;

/// <summary>
///     Many of API matches are infinite<br/>
///     Eg. https://osu.ppy.sh/community/matches/41478279<br/>
///     https://osu.ppy.sh/community/matches/42992777
/// </summary>
public static class InfiniteMatchesBug
{
    /// <summary>
    ///     5 hours are based on nothing. I assumed no one would play a 5 hour map...
    /// </summary>
    private static readonly TimeSpan INFINITE_MATCH_TIMESPAN = TimeSpan.FromHours(5);

    public static bool IsMatchInfinite(DateTimeOffset lastTimestamp)
    {
        //We assume `now` is greater than `event timestamp`
        return DateTimeOffset.UtcNow - lastTimestamp > INFINITE_MATCH_TIMESPAN;
    }
}