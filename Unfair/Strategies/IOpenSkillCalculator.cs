using SkillIssue.Domain.Unfair.Entities;

namespace Unfair.Strategies;

public interface IOpenSkillCalculator
{
    public List<double> PredictWinHeadOnHead(params Rating[] ratings);
    public List<double> PredictWinTeamOnTeam(params Rating[][] ratings);
    public List<(int rank, double prediction)> PredictRankHeadOnHead(params Rating[] ratings);
}