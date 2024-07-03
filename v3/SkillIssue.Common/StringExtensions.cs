namespace SkillIssue.Common;

public static class StringExtensions
{
    public static bool IsNullOrWhiteSpace(this string source) => string.IsNullOrWhiteSpace(source);
    public static bool IsNotNullOrWhiteSpace(this string source) => !string.IsNullOrWhiteSpace(source);
}