namespace SkillIssue.Domain.Skillset;

public interface ISkillsetFactory
{
    public IReadOnlyList<Skillset> GetBeatmapSkillsets(Modification.Modification modification,
        BeatmapDifficulty? difficulty);
}