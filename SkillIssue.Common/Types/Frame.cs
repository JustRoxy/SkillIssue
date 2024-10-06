namespace SkillIssue.Common.Types;

public class Frame<T>(T value, byte[] data)
{
    public T Value { get; } = value;
    public byte[] Data { get; } = data;
}