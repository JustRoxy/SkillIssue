using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SkillIssue.Domain.TGML.Entities;

public enum TgmlMatchStatus
{
    Ongoing,
    Completed,
    Gone
}

public class TgmlMatch : BaseEntity
{
    private string _name = null!;

    public int MatchId { get; set; }
    public string Name
    {
        get => _name;
        set => _name = value.Replace("\0", "");
    }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public TgmlMatchStatus MatchStatus { get; set; }

    public byte[]? CompressedJson { get; set; }

    public IList<TgmlPlayer> Players { get; set; } = null!;

    public async Task Serialize(JsonNode jsonObject)
    {
        var decompressed = Encoding.UTF8.GetBytes(jsonObject.ToString());
        var mb = decompressed.Length * sizeof(byte) / 1024.0 / 1024.0;
        var compressionLevel = mb switch
        {
            < 10 => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Optimal
        };

        using var inputStream = new MemoryStream(decompressed);
        using var outputStream = new MemoryStream();
        await using (var brotli = new BrotliStream(outputStream, compressionLevel))
        {
            await inputStream.CopyToAsync(brotli);
        }

        CompressedJson = outputStream.ToArray();
    }

    public async Task<JsonObject?> Deserialize()
    {
        if (CompressedJson is null) return null;

        using var inputStream = new MemoryStream(CompressedJson);
        using var outputStream = new MemoryStream();

        await using (var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress))
        {
            await brotliStream.CopyToAsync(outputStream);
        }


        return JsonSerializer.Deserialize<JsonObject>(outputStream.ToArray());
    }
}