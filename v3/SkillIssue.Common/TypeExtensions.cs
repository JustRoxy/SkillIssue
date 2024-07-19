namespace SkillIssue.Common;

public static class TypeExtensions
{
    public static string GetUnderlyingTypeName<T>(this T value) where T : notnull
    {
        return value.GetType().Name;
    }
}