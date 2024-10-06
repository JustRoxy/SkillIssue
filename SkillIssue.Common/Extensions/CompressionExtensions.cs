using System.IO.Compression;

namespace SkillIssue.Common.Extensions;

public static class CompressionExtensions
{
    public static byte[] BrotliCompress(this byte[] data)
    {
        using var memoryStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();

        using (var compressor = new BrotliStream(outputStream, CompressionLevel.Optimal))
        {
            memoryStream.CopyTo(compressor);
        }

        return outputStream.ToArray();
    }

    public static byte[] BrotliDecompress(this byte[] data)
    {
        using var memoryStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();

        using (var decompressor = new BrotliStream(memoryStream, CompressionMode.Decompress))
        {
            decompressor.CopyTo(outputStream);
        }

        return outputStream.ToArray();
    }
}