using SkillIssue.Domain;

namespace SkillIssue.Repository;

public interface IMatchFrameRepository
{
    public Task<IEnumerable<MatchFrameData>> GetMatchFramesWithBulk(IList<int> matchIds,
        CancellationToken cancellationToken);
    public Task CacheFrame(MatchFrameData matchFrameData, CancellationToken cancellationToken);
}