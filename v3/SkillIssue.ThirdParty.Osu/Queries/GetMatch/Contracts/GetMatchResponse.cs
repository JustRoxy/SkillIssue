using SkillIssue.Common;

namespace SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts;

public class GetMatchResponse(
    Func<MatchFrameRaw, Task<MatchFrameRaw>> getNext,
    Func<MatchFrameRaw, bool> hasNext,
    Func<ValueTask> dispose)
    : LambdaEnumerable<MatchFrameRaw>(getNext, hasNext, dispose);