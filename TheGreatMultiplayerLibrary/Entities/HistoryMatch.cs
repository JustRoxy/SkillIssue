using System.Text.Json.Serialization;

namespace TheGreatMultiplayerLibrary.Entities;

public class HistoryMatch
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("start_time")] public DateTime StartTime { get; set; }
    [JsonPropertyName("end_time")] public DateTime? EndTime { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = null!;
}