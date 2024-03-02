namespace SkillIssue.Domain.Events.Beatmaps;

public class BeatmapProcessed : BaseEvent
{
    public required int BeatmapId { get; set; }
}