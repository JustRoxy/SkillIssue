namespace SkillIssue.Beatmaps.Models;

public class Beatmap
{
    public required int BeatmapId { get; set; }
    public required string Title { get; set; }
    public required string Artist { get; set; }
    public required string Difficulty { get; set; }
    public required string Version { get; set; }
}