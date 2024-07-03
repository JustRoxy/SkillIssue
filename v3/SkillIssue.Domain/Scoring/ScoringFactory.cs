namespace SkillIssue.Domain.Scoring;

public class ScoringFactory : IScoringFactory
{
    /// <summary>
    ///     Use all scorings for a game
    /// </summary>
    public IReadOnlySet<Scoring> GetGameScorings(Game game)
    {
        return Scoring.All;
    }
}