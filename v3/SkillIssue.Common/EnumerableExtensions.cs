using Microsoft.Extensions.Logging;

namespace SkillIssue.Common;

public static class EnumerableExtensions
{
    public static IEnumerable<T> WithProgressLogging<T>(this IEnumerable<T> source, ILogger logger, string name)
    {
        var list = source.ToArray();

        var length = list.Length;
        var i = 0;

        foreach (var value in list)
        {
            i++;
            logger.LogInformation("[Progress {Name}]: {Current} | {Total} ({Completion:P})", name, i, length,
                (double)i / length);
            yield return value;
        }
    }
}