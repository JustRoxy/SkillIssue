namespace SkillIssue.Domain.Scoring;

public interface IScoring
{
    public IEnumerable<Score> Score(IEnumerable<Score> scores);
}