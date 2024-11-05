namespace SkillIssue.Math;

public static class Approximate
{
    public static double Iterative(double pointToInterpolate, double startValue, int iterations, double epsilon, Func<double, double> mappingFunction)
    {
        double value = startValue;
        for (int i = 0; i < iterations; i++)
        {
            var left = mappingFunction(value - epsilon);
            var right = mappingFunction(value + epsilon);

            var dLeft = double.Abs(pointToInterpolate - left);
            var dRight = double.Abs(pointToInterpolate - right);

            if (dLeft < dRight) value = left;
            if (dRight < dLeft) value = right;
        }

        return value;
    }
}