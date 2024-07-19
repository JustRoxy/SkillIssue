using System.Diagnostics;
using MediatR;
using SkillIssue.Application.Commands.Stage2UpdateInProgressMatches.Contracts;
using SkillIssue.Application.Commands.Stage3ExtractDataInCompletedMatch.Contracts;
using SkillIssue.Application.Commands.Stage4UpdateDataInExtractedMatch.Contracts;
using SkillIssue.Common;

namespace SkillIssue.Scheduler;

public class JobScheduler(IMediator mediator, ILogger<JobScheduler> logger) : BackgroundService
{
    private static readonly ScheduleTask[] Tasks =
    [
        // new ScheduleTask(new FindNewMatchesRequest(), TimeSpan.FromMinutes(2))
        // new ScheduleTask(new UpdateInProgressMatchesRequest(), TimeSpan.FromMinutes(2)),
        // new ScheduleTask(new ExtractDataInCompletedMatchRequest(), TimeSpan.FromMinutes(2))
        new ScheduleTask(new UpdateDataInExtractedMatchRequest(), TimeSpan.FromMinutes(2))
    ];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            CycleThoughTasks(stoppingToken);
        }

        return Task.CompletedTask;
    }

    private void CycleThoughTasks(CancellationToken cancellationToken)
    {
        foreach (var task in Tasks)
        {
            if (task.Task is null) HandleTaskRun(task, cancellationToken);
            else HandleTaskCompletion(task);
        }
    }

    private void HandleTaskRun(ScheduleTask task, CancellationToken cancellationToken)
    {
        var elapsedSinceEnd = Stopwatch.GetElapsedTime(task.CompletedAt);
        if (elapsedSinceEnd < task.Schedule) return;

        logger.LogInformation("Starting scheduled {TaskName}", task.Request.GetType().Name);
        StartScheduledTask(task, cancellationToken);
    }

    private void HandleTaskCompletion(ScheduleTask task)
    {
        if (task.Task!.Status == TaskStatus.Running) return;
        try
        {
            task.Task.GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "SCHEDULER CRITICAL ERROR: {TaskName} failed", task.Request.GetUnderlyingTypeName());
        }

        LogAndResetScheduleTask(task);
    }

    private void StartScheduledTask(ScheduleTask task, CancellationToken cancellationToken)
    {
        task.StartedAt = Stopwatch.GetTimestamp();
        task.Task = mediator.Send(task.Request, cancellationToken);
        task.Task.ConfigureAwait(false);
    }

    private void LogAndResetScheduleTask(ScheduleTask task)
    {
        logger.LogInformation("Invoked scheduled task {TaskName} in {Elapsed:N2}ms",
            task.Request.GetUnderlyingTypeName(),
            Stopwatch.GetElapsedTime(task.StartedAt).TotalMilliseconds);
        task.CompletedAt = Stopwatch.GetTimestamp();
        task.Task = null;
    }
}