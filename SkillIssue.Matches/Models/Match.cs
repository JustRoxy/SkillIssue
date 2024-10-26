using System.Text.RegularExpressions;

namespace SkillIssue.Matches.Models;

public partial class Match
{

    public int MatchId { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; } //null endTime means ongoing match


    public List<MatchFrame> Frames { get; set; } = [];

}