namespace SkillIssue.Domain.Freemods;

public interface IScoreGrouper
{
    public IEnumerable<IGrouping<Modification.Modification, Score>> Group(IReadOnlyList<Score> scores);
}