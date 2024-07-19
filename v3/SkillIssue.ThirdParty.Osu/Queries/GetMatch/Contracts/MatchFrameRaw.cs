using System.Text.Json;
using SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts.Match;

namespace SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts;

public class MatchFrameRaw
{
    public MatchFrameRaw(byte[] rawFrame, long cursor)
    {
        RawFrame = rawFrame;
        Cursor = cursor;
        Frame = ToFrame(RawFrame);
    }

    public byte[] RawFrame { get; set; }
    public long Cursor { get; set; }
    public DateTimeOffset? LastEventTimestamp { get; set; } = null;
    public MatchFrame? Frame { get; set; }

    private MatchFrame? ToFrame(byte[] rawFrame)
    {
        using var memoryStream = new MemoryStream(rawFrame);
        return JsonSerializer.Deserialize<MatchFrame>(memoryStream);
    }
}