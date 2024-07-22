using SkillIssue.Domain;

namespace SkillIssue.ThirdParty.OsuGame;

public interface IDifficultyCalculator
{
    public IEnumerable<BeatmapDifficulty> CalculateBeatmapDifficulty(int beatmapId, byte[] content,
        CancellationToken cancellationToken);
}