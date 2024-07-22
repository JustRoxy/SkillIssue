using SkillIssue.Common;
using SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts.Match;

namespace SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts;

public class MatchFrameRaw : JsonSourcedData<MatchFrame>
{
    public MatchFrameRaw(byte[] rawFrame, long cursor) : base(rawFrame)
    {
        Cursor = cursor;
    }

    public long Cursor { get; set; }
    public DateTimeOffset? LastEventTimestamp { get; set; } = null;
}