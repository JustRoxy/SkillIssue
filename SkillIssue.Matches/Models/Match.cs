using System.Text.RegularExpressions;

namespace SkillIssue.Matches.Models;

public partial class Match
{
    private static readonly Regex TournamentNameParser = TournamentNameRegex();

    public int MatchId { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; } //null endTime means ongoing match

    public bool IsNameInTournamentFormat => TournamentNameParser.IsMatch(Name);

    public List<MatchFrame> Frames { get; set; } = [];

    [GeneratedRegex(@"(?'acronym'.+):\s*(?'red'\(*.+\)*)\s*vs\s*(?'blue'\(*.+\)*)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "")]
    private static partial Regex TournamentNameRegex();
}