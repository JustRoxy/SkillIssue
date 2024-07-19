namespace SkillIssue.Infrastructure.Repositories.BeatmapRepository.Contracts;

public class BeatmapRecord
{
    public int BeatmapId { get; set; }
    public int Status { get; set; }
    public byte[]? Content { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
}