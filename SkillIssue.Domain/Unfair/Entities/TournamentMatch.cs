using System.Text.RegularExpressions;

namespace SkillIssue.Domain.Unfair.Entities;

public enum TournamentMatchType
{
    Tryouts,
    Qualifications,
    Standard
}

public class TournamentMatch
{
    private static readonly Regex TournamentNameParser =
        new(@"(?'acronym'.+):\s*(?'red'\(*.+\)*)\s*vs\s*(?'blue'\(*.+\)*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LobbyParser =
        new("(Lobby|Match)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TryoutsParser = new("Tryouts", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex QualificationsParser =
        new("Qual", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public int MatchId { get; set; }
    public string Name { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Acronym { get; set; }
    public string? RedTeam { get; set; }
    public string? BlueTeam { get; set; }

    public IList<Score>? Scores { get; set; } = null!;

    public static TournamentMatchType GetTournamentMatchType(TournamentMatch match)
    {
        ArgumentNullException.ThrowIfNull(match.RedTeam);
        if (TryoutsParser.IsMatch(match.RedTeam)) return TournamentMatchType.Tryouts;

        if (!LobbyParser.IsMatch(match.Name))
            return TournamentMatchType.Standard;

        if (TryoutsParser.IsMatch(match.Name)) return TournamentMatchType.Tryouts;
        if (QualificationsParser.IsMatch(match.Name)) return TournamentMatchType.Qualifications;

        return TournamentMatchType.Standard;
    }

    public static (string acronym, string redTeam, string blueTeam)? GetTournamentMatchInfoByName(string name)
    {
        var matches = TournamentNameParser.Match(name);
        if (!matches.Success) return null;

        return (matches.Groups["acronym"].Value,
            matches.Groups["red"].Value.Trim(['(', ')', ' ']),
            matches.Groups["blue"].Value.Trim(['(', ')', ' ']));
    }
}