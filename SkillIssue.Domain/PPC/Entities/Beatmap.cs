using System.IO.Compression;

namespace SkillIssue.Domain.PPC.Entities;

public enum BeatmapStatus
{
    NeedsUpdate,
    Ok,
    NotFound,
    Incalculable
}

public class Beatmap
{
    public int BeatmapId { get; set; }
    public string? Artist { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }

    public string FullName =>
        $"{Artist ?? "Unknown artist"} - {Name ?? "Unknown song name"} [{Version ?? "Unknown version"}]";

    public BeatmapStatus Status { get; set; }
    public byte[]? CompressedBeatmap { get; set; }

    public IList<BeatmapPerformance> Performances { get; set; } = null!;

    public async Task<byte[]> Decompress()
    {
        if (CompressedBeatmap is null) return [];

        using var input = new MemoryStream(CompressedBeatmap);
        using var output = new MemoryStream();
        await using (var brotli = new BrotliStream(input, CompressionMode.Decompress))
        {
            await brotli.CopyToAsync(output);
        }

        return output.ToArray();
    }
}