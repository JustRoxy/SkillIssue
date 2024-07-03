namespace SkillIssue.Domain.Scoring;

public interface IScoringFactory
{
    public IReadOnlySet<Scoring> GetGameScorings(Game game);
}