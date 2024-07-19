using Microsoft.Extensions.Logging;

namespace SkillIssue.Common;

public static class EnumerableExtensions
{
    //TODO: somehow remove possible multiple enumeration?
    public static IEnumerable<T> WithProgressLogging<T>(this IEnumerable<T> source, ILogger logger, string name)
    {
        var length = source.Count();
        var i = 0;
        foreach (var value in source)
        {
            i++;
            logger.LogInformation("[Progress {Name}]: {Current} | {Total} ({Completion:P})", name, i, length,
                (double)i / length);
            yield return value;
        }
    }
}