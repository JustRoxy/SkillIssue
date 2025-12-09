namespace SkillIssue.Domain.Extensions;

public static class EnumerableExtensions
{
    public static IQueryable<TE> IfTransforming<T, TE>(this IQueryable<T> source, bool flag,
        Func<IQueryable<T>, IQueryable<TE>> ifTrue, Func<IQueryable<T>, IQueryable<TE>> ifFalse)
    {
        return flag ? ifTrue(source) : ifFalse(source);
    }
    public static IQueryable<T> If<T>(this IQueryable<T> source, bool flag,
        Func<IQueryable<T>, IQueryable<T>> ifTrue, Func<IQueryable<T>, IQueryable<T>> ifFalse)
    {
        return flag ? ifTrue(source) : ifFalse(source);
    }

    public static IQueryable<T> Case<T>(this IQueryable<T> source, bool flag,
        Func<IQueryable<T>, IQueryable<T>> apply)
    {
        return flag ? apply(source) : source;
    }
}