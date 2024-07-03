namespace SkillIssue.Common;

public static class ArrayExtensions
{
    public static double GetPhysicalSizeInMegabytes(this byte[] array)
    {
        return array.Length * sizeof(byte) / 1024.0 / 1024.0;
    }
}