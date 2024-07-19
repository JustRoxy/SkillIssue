using Microsoft.Extensions.Logging;

namespace SkillIssue.Common.Utils;

public static class TimeMeasuring
{
    public static async Task MeasureAsync(ILogger logger, string name, Func<Task> task)
    {
        using var context = new TimeMeasuringContext(name,
            (s, span) =>
            {
                logger.LogInformation("Measured time for {MeasureName} is {Elapsed:N2}ms", s, span.TotalMilliseconds);
            });

        await task();
    }

    public static async Task<T> MeasureAsync<T>(ILogger logger, string name, Func<Task<T>> task)
    {
        using var context = new TimeMeasuringContext(name,
            (s, span) =>
            {
                logger.LogInformation("Measured time for {MeasureName} is {Elapsed:N2}ms", s, span.TotalMilliseconds);
            });

        return await task();
    }
}