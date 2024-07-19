using SkillIssue.ThirdParty.Osu.Queries.GetMatch.Contracts;
using SkillIssue.ThirdParty.Osu.Queries.GetMatchPage.Contracts;

namespace SkillIssue.ThirdParty.Osu;

public interface IOsuClient
{
    public Task<byte[]?> GetBeatmapContent(int beatmapId, CancellationToken cancellationToken);
    public Task<GetMatchPageResponse> GetNextMatchPage(long lastMatch, CancellationToken cancellationToken);

    public GetMatchResponse GetMatchAsAsyncEnumerable(GetMatchRequest request,
        CancellationToken cancellationToken);
}