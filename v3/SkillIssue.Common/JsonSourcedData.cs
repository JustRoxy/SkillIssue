using System.Text.Json;

namespace SkillIssue.Common;

public class JsonSourcedData<T>
{
    public JsonSourcedData(byte[] rawData)
    {
        RawData = rawData;
        try
        {
            Representation = JsonSerializer.Deserialize<T>(rawData)!;
        }
        catch
        {
            Representation = default;
        }
    }

    public byte[] RawData { get; set; }
    public T? Representation { get; set; }
}