using System.Security.Cryptography;

namespace SkillIssue.Common;

public static class CryptographyExtensions
{
    public static byte[] MurMurHash3(this byte[] source)
    {
        var murmur = new MurMur3();
        return murmur.ComputeHash(source);
    }

    public static byte[] MD5Hash(this byte[] source)
    {
        return MD5.HashData(source);
    }

    public static byte[] SHA256Hash(this byte[] source)
    {
        return SHA256.HashData(source);
    }
}