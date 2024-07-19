using System.IO.Compression;

namespace SkillIssue.Common;

public static class BrotliCompressionExtensions
{
    /// <summary>
    ///     These numbers are pulled from a thin-air
    /// </summary>
    public static CompressionLevel SuitableBrotliCompressionLevel(this byte[] array)
    {
        var compressionLevel = array.GetPhysicalSizeInMegabytes() switch
        {
            < 10 => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Optimal
        };

        return compressionLevel;
    }

    public static async Task<byte[]> BrotliCompress(this byte[] array, CompressionLevel compressionLevel,
        CancellationToken cancellationToken)
    {
        using var inputStream = new MemoryStream(array);
        using var outputStream = new MemoryStream();
        await using (var brotli = new BrotliStream(outputStream, compressionLevel))
        {
            await inputStream.CopyToAsync(brotli, cancellationToken);
        }

        return outputStream.ToArray();
    }

    public static async Task<byte[]> BrotliDecompress(this byte[] array, CancellationToken cancellationToken)
    {
        using var inputStream = new MemoryStream(array);
        using var outputStream = new MemoryStream();

        await using (var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress))
        {
            await brotliStream.CopyToAsync(outputStream, cancellationToken);
        }

        return outputStream.ToArray();
    }
}