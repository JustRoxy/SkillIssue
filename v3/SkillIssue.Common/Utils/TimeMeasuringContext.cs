using System.Diagnostics;

namespace SkillIssue.Common.Utils;

public class TimeMeasuringContext(string name, Action<string, TimeSpan> onFinish) : IDisposable
{
    private readonly long _startTimestamp = Stopwatch.GetTimestamp();

    private void Finish()
    {
        onFinish(name, Stopwatch.GetElapsedTime(_startTimestamp));
    }

    public void Dispose()
    {
        Finish();
    }
}