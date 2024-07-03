namespace SkillIssue.Common;

public static class EnumExtensions
{
    public static HashSet<TValue> GetAllValues<TEnum, TValue>(Func<TEnum, TValue> constructor)
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>()
            .Select(constructor)
            .ToHashSet();
}