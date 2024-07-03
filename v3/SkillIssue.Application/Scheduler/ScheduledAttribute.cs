namespace SkillIssue.Application.Scheduler;

public class ScheduledAttribute(int repeatAfterInSeconds) : Attribute
{
    public TimeSpan RepeatAfter { get; } = TimeSpan.FromSeconds(repeatAfterInSeconds);
}