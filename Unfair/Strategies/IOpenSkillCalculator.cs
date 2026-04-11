// Copyright (c) JustRoxy <justroxyosu@inbox.ru>. Licensed under the GPLv3 License.
// See the LICENSE file in the repository root for full license text.

using SkillIssue.Domain.Unfair.Entities;

namespace Unfair.Strategies;

public interface IOpenSkillCalculator
{
    public List<double> PredictWinHeadOnHead(params Rating[] ratings);
    public List<double> PredictWinTeamOnTeam(params Rating[][] ratings);
    public List<(int rank, double prediction)> PredictRankHeadOnHead(params Rating[] ratings);
}