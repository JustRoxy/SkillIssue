using SkillIssue.Application.Commands.Stage3ExtractDataInCompletedMatch;
using SkillIssue.Application.Commands.Stage4UpdateDataInExtractedMatch;
using SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts.Match;

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


    /// <summary>
    ///     Used in <see cref="MergeDataInUpdatedMatchHandler"/>
    /// </summary>
    public Task MergeData(int matchId, CancellationToken cancellationToken);
}