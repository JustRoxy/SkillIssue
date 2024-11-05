namespace SkillIssue.Math;

public static class Completion
{
    public static double CalculateScoreV2Completion(double totalScore)
    {
        const double a = 700_000d;
        const double b = 300_000d;
        return (-b + System.Math.Sqrt(b * b - 4 * a * -totalScore)) / (2 * a);
    }
}