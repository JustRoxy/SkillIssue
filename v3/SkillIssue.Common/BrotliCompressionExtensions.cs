using System.IO.Compression;

namespace SkillIssue.Common;

public static class BrotliCompressionExtensions
{
    public static async Task<byte[]> BrotliCompress(this byte[] array, CompressionLevel compressionLevel)
    {
        using var inputStream = new MemoryStream(array);
        using var outputStream = new MemoryStream();
        await using (var brotli = new BrotliStream(outputStream, compressionLevel))
        {
            await inputStream.CopyToAsync(brotli);
        }

        return outputStream.ToArray();
    }

    public static async Task<byte[]> BrotliDecompress(this byte[] array)
    {
        using var inputStream = new MemoryStream(array);
        using var outputStream = new MemoryStream();

        await using (var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress))
        {
            await brotliStream.CopyToAsync(outputStream);
        }

        return outputStream.ToArray();
    }
}