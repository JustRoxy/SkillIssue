using SkillIssue.Matches.Contracts;
using SkillIssue.Matches.Contracts.Events;

namespace SkillIssue.Matches.Extensions;

public static class MatchExtensions
{

    /// <summary>
    ///     We should stop before the ongoing game event
    /// </summary>
    public static long FindNextEventIdCursor(this MatchResponse match, long defaultValue)
    {
        //If there are no ongoing game we are ready to skip this frame
        if (match.CurrentGameId is null) return FindMaxEventId(match, defaultValue);

        return FindEventBeforeOngoingGame(match, defaultValue);
    }

    public static bool ConsumedAllEvents(this MatchResponse match)
    {
        if (match.Events.Count == 0) return false;

        return match.LatestEventId == match.Events.Max(x => x.EventId);
    }

    private static long FindMaxEventId(MatchResponse matchFrame, long defaultValue)
    {
        if (matchFrame.Events.Count == 0) return defaultValue;

        return matchFrame.Events.Max(ev => ev.EventId);
    }

    private static long FindEventBeforeOngoingGame(MatchResponse frame, long defaultValue)
    {
        //If there are no events at the frame it means either API returned an empty response, or we hit the end on the match events
        //We assume the positive result: we hit the end of the match events
        if (frame.Events.Count == 0) return defaultValue;

        //If there are only one event - then check if it's an ongoing game or not.
        if (frame.Events.Count == 1 && EventIsOngoingGame(frame, frame.Events[0]))
            return defaultValue;

        for (var i = 0; i < frame.Events.Count - 1; i++)
        {
            var current = frame.Events[i].EventId;
            var next = frame.Events[i + 1];

            if (EventIsOngoingGame(frame, next)) return current;
        }

        return FindMaxEventId(frame, defaultValue);
    }

    private static bool EventIsOngoingGame(MatchResponse frame, MatchEvent @event)
    {
        return @event.Game?.Id == frame.CurrentGameId;
    }

    public static MatchResponse Merge(this MatchResponse source, MatchResponse dest)
    {
        return MatchResponse.Merge(source, dest);
    }
}