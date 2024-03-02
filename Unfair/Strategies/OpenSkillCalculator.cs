using OpenSkill.Types;
using Rating = SkillIssue.Domain.Unfair.Entities.Rating;

namespace Unfair.Strategies;

public class OpenSkillCalculator(OpenSkill.OpenSkill openSkill) : IOpenSkillCalculator
{
    public List<double> PredictWinHeadOnHead(params Rating[] ratings)
    {
        var teams = ratings.Select(x => Team.With(new OpenSkill.Types.Rating(x.Mu, x.Sigma, x.PlayerId))).ToList();
        return openSkill.PredictWin(teams).ToList();
    }

    public List<double> PredictWinTeamOnTeam(params Rating[][] ratings)
    {
        var teams = ratings.Select(x =>
            Team.With(x.Select(z => new OpenSkill.Types.Rating(z.Mu, z.Sigma, z.PlayerId)).ToArray())).ToList();
        return openSkill.PredictWin(teams).ToList();
    }

    public List<(int rank, double prediction)> PredictRankHeadOnHead(params Rating[] ratings)
    {
        var teams = ratings.Select(x => Team.With(new OpenSkill.Types.Rating(x.Mu, x.Sigma, x.PlayerId))).ToList();
        return openSkill.PredictRank(teams);
    }
}