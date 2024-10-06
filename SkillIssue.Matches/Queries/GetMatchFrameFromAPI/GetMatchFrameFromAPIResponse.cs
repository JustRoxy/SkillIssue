using SkillIssue.Common.Types;
using SkillIssue.Matches.Contracts;

namespace SkillIssue.Matches.Queries.GetMatchFrameFromAPI;

public class GetMatchFrameFromAPIResponse
{
    public required Frame<MatchResponse>? Frame { get; set; }
    public DateTimeOffset? LastTimestamp { get; set; }
    public long Cursor { get; set; }
}