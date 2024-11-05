using System.Linq.Expressions;
using System.Reflection;

namespace SkillIssue.Math;

public static class Extensions
{
    public static IEnumerable<T> Normalize<T>(this IEnumerable<T> source, Expression<Func<T, double>> prop, double minValue, double maxValue)
    {
        var propertyGetter = prop.Compile();

        var memberExpression = prop.Body as MemberExpression;
        if (prop.Body is UnaryExpression { NodeType: ExpressionType.Convert } unaryExpression)
        {
            memberExpression = unaryExpression.Operand as MemberExpression;
        }

        if (memberExpression == null)
            throw new ArgumentException("Expression must be a property or field", nameof(prop));

        var member = memberExpression.Member;
        var isProperty = member is PropertyInfo;
        var sList = source.ToList();
        var values = sList.Select(propertyGetter).ToList();

        var min = values.Min();
        var max = values.Max();
        var range = max - min;

        if (range == 0) range = 1;

        foreach (var item in sList)
        {
            var originalValue = propertyGetter(item);
            var normalizedValue = (originalValue - min) / range;

            normalizedValue = normalizedValue * (maxValue - minValue) + minValue;
            if (isProperty)
                ((PropertyInfo)member).SetValue(item, normalizedValue);
            else
                ((FieldInfo)member).SetValue(item, normalizedValue);
        }

        return sList;
    }
}