namespace SkillIssue.Common.Extensions;

public static class LinqExtensions
{
    public static IEnumerable<IEnumerable<T>> LinearOverlapChunk<T>(this IEnumerable<T> source, int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);

        //I feel it could be implemented lazily but idc
        var materialized = source.ToList();

        for (int i = 0; i < materialized.Count - size; i++)
        {
            var spline = materialized[i..(i + size)];

            yield return spline;
        }
    }
}