namespace SkillIssue.Domain;

public class Game
{
    public long GameId { get; set; }
    public long BeatmapId { get; set; }

    public List<Score> Scores { get; set; } = [];
}