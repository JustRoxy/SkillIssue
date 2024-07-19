using MediatR;

namespace SkillIssue.Scheduler;

public class ScheduleTask
{
    public ScheduleTask(IRequest request, TimeSpan schedule)
    {
        Request = request;
        Schedule = schedule;
    }

    public IRequest Request { get; set; }
    public TimeSpan Schedule { get; set; }

    public Task? Task { get; set; } = null;
    public long StartedAt { get; set; }
    public long CompletedAt { get; set; }
}