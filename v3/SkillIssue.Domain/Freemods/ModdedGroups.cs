namespace SkillIssue.Domain.Freemods;

/// <summary>
///     Score grouping is a process of splitting freemod game scores by their respective mod<br/>
///     Hidden players play against Hidden players<br/>
///     Hard Rock players play against Hard Rock players<br/>
///     It's required because we can't compare hidden beatmap difficulty with hard rock beatmap difficulty 
/// </summary>
public class ModdedGroups : IScoreGrouper
{
    public IEnumerable<IGrouping<Modification.Modification, Score>> Group(IReadOnlyList<Score> scores)
    {
        return scores.GroupBy(score => score.Modification);
    }
}