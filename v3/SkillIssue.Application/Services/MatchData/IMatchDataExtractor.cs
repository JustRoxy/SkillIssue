using SkillIssue.Application.Commands.Stage3ExtractDataInCompletedMatch;
using SkillIssue.Application.Commands.Stage4UpdateDataInExtractedMatch;
using SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts.Match;

namespace SkillIssue.Application.Services.MatchData;

public interface IMatchDataExtractor
{
    /// <summary>
    ///     Used in <see cref="ExtractDataInCompletedMatchHandler"/>. Also can be used for real-time data updates
    /// </summary>
    public Task ExtractData(IEnumerable<MatchFrame> frames, CancellationToken cancellationToken);

    /// <summary>
    ///     Used in <see cref="UpdateDataInExtractedMatchHandler"/>
    /// </summary>
    public Task UpdateData(int matchId, CancellationToken cancellationToken);
}