using SkillIssue.ThirdParty.API.Osu.Queries.GetMatch.Contracts;
using SkillIssue.ThirdParty.API.Osu.Queries.GetMatchPage.Contracts;

namespace SkillIssue.ThirdParty.API.Osu;

public interface IOsuClient
{
    public Task<byte[]?> GetBeatmapContent(int beatmapId, CancellationToken cancellationToken);
    public Task<GetMatchPageResponse> GetNextMatchPage(long lastMatch, CancellationToken cancellationToken);

    public GetMatchResponse GetMatchAsAsyncEnumerable(GetMatchRequest request,
        CancellationToken cancellationToken);
}