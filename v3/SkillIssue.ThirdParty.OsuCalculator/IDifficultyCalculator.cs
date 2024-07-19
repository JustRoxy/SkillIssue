using SkillIssue.Domain;

namespace SkillIssue.ThirdParty.OsuCalculator;

public interface IDifficultyCalculator
{
    public IEnumerable<BeatmapDifficulty> CalculateBeatmapDifficulty(int beatmapId, byte[] content,
        CancellationToken cancellationToken);
}